#!/usr/bin/env bash

set -o errexit
set -o pipefail
set -o nounset

TEST_FILE="${1:-tests.wonders.json}"
TIMEOUT="${2:-10}"
REPORT_FILE="${3:-wonders_report.$(date +%Y%m%d_%H%M%S).json}"
INSECURE="${INSECURE:-false}"

if ! command -v curl >/dev/null 2>&1; then
  echo "ERROR: curl is required." >&2
  exit 2
fi
if ! command -v jq >/dev/null 2>&1; then
  echo "ERROR: jq is required." >&2
  exit 2
fi

CURL_INSECURE_FLAG=""
if [[ "$INSECURE" == "true" ]]; then
  CURL_INSECURE_FLAG="--insecure"
  echo "Note: curl will run with --insecure (INSECURE=true)."
fi

if ! jq -e '.' "$TEST_FILE" >/dev/null 2>&1; then
  echo "ERROR: cannot parse test file '$TEST_FILE' as JSON" >&2
  exit 2
fi

RESULTS=()
idx=0
TOTAL=$(jq 'length' "$TEST_FILE")

jq -c '.[]' "$TEST_FILE" | while read -r test; do
  idx=$((idx+1))
  name=$(jq -r '.name // "test_'$idx'"' <<<"$test")
  method=$(jq -r '.method // "GET"' <<<"$test")
  url=$(jq -r '.url' <<<"$test")
  headers_json=$(jq -r '.headers // {}' <<<"$test")
  body=$(jq -r 'if has("body") then .body else empty end' <<<"$test")
  expect_status=$(jq -r '.expect_status // "2xx"' <<<"$test")
  expect_contains=$(jq -r '.expect_contains // ""' <<<"$test")
  max_latency_ms=$(jq -r '.max_latency_ms // 5000' <<<"$test")
  retries=$(jq -r '.retries // 0' <<<"$test")
  follow_location=$(jq -r '.follow_location // false' <<<"$test")

  CURL_HDR_ARGS=()
  if jq -e 'type=="object"' <<<"$headers_json" >/dev/null 2>&1; then
    echo "$headers_json" | jq -r 'to_entries[] | @base64' | while read -r h; do
      kv=$(echo "$h" | base64 --decode)
      k=$(jq -r '.key' <<<"$kv")
      v=$(jq -r '.value' <<<"$kv")
      CURL_HDR_ARGS+=( -H "$k: $v" )
    done
  fi

  temp_resp=$(mktemp)
  attempt=0
  success=false
  last_err=""
  http_code="0"
  latency_ms=0
  bytes=0

  while (( attempt <= retries )); do
    attempt=$((attempt+1))
    start_time=$(date +%s%3N)
    set +o errexit
    curl_out=$(curl -sS -w '\n%{http_code} %{time_total} %{size_download}' \
      -X "$method" "${CURL_HDR_ARGS[@]}" $CURL_INSECURE_FLAG \
      --max-time "$TIMEOUT" \
      ${body:+--data-binary} ${body:+$body} \
      -D - \
      --output "$temp_resp" \
      "$url" 2>&1) || CURL_EXIT=$? && CURL_EXIT=${CURL_EXIT:-0}
    CURL_EXIT=${CURL_EXIT:-0}
    set -o errexit
    end_time=$(date +%s%3N)
    lastline=$(printf "%s" "$curl_out" | tail -n1)
    http_code=$(awk '{print $1}' <<<"$lastline")
    time_total=$(awk '{print $2}' <<<"$lastline")
    bytes=$(awk '{print $3}' <<<"$lastline")
    latency_ms=$(awk "BEGIN {printf \"%d\", $time_total * 1000}")

    if [[ "$CURL_EXIT" -ne 0 ]]; then
      last_err="curl_failed_exit_${CURL_EXIT} -> $(printf '%s' "$curl_out" | head -n1)"
      echo "[$idx/$TOTAL] $name attempt $attempt: curl error ($CURL_EXIT)"
      if (( attempt <= retries )); then sleep 1; continue; else break; fi
    fi

    ok_status=false
    if [[ "$expect_status" =~ ^[0-9]{3}$ ]]; then
      [[ "$http_code" == "$expect_status" ]] && ok_status=true
    elif [[ "$expect_status" =~ ^[23]xx$ ]]; then
      prefix=${expect_status:0:1}
      if [[ "$http_code" =~ ^$prefix[0-9][0-9]$ ]]; then ok_status=true; fi
    elif [[ "$expect_status" =~ ^([0-9]{3})-([0-9]{3})$ ]]; then
      low=${BASH_REMATCH[1]}; high=${BASH_REMATCH[2]}
      if (( http_code >= low && http_code <= high )); then ok_status=true; fi
    fi

    resp_body=$(cat "$temp_resp" 2>/dev/null || true)
    json_valid=true
    if [[ -n "$resp_body" ]]; then
      if ! jq -e '.' "$temp_resp" >/dev/null 2>&1; then
        json_valid=false
      fi
    else
      json_valid=false
    fi

    if [[ "$http_code" == "204" ]]; then
      json_valid=true
    fi

    contains_ok=true
    if [[ -n "$expect_contains" ]]; then
      if ! grep -qF "$expect_contains" "$temp_resp"; then
        contains_ok=false
      fi
    fi

    if [[ "$method" == "POST" && "$follow_location" == "true" && "$http_code" =~ ^2[0-9][0-9]$ ]]; then
      location=$(printf "%s" "$curl_out" | sed -n '1,/'$lastline'/p' | grep -i '^Location:' | head -n1 | sed -E 's/^[Ll]ocation:\s*//')
      if [[ -n "$location" ]]; then
        echo "Following Location: $location"
        loc_temp=$(mktemp)
        loc_lastline=$(curl -sS -w '\n%{http_code} %{time_total} %{size_download}' -D - --output "$loc_temp" $CURL_INSECURE_FLAG --max-time "$TIMEOUT" "$location" | tail -n1)
        loc_http=$(awk '{print $1}' <<<"$loc_lastline")
        if [[ "$loc_http" =~ ^2[0-9][0-9]$ ]]; then
          rm -f "$loc_temp"
        else
          last_err="created_location_get_failed:${loc_http}"
        fi
      fi
    fi

    if [[ "$ok_status" == "true" && "$contains_ok" == "true" && "$latency_ms" -le "$max_latency_ms" ]]; then
      success=true
      last_err=""
      break
    else
      reasons=()
      if [[ "$ok_status" != "true" ]]; then reasons+=("unexpected_status:${http_code}"); fi
      if [[ "$contains_ok" != "true" ]]; then reasons+=("missing_expected_text"); fi
      if [[ "$latency_ms" -gt "$max_latency_ms" ]]; then reasons+=("slow_response:${latency_ms}ms>${max_latency_ms}ms"); fi
      last_err=$(IFS=','; echo "${reasons[*]}")
    fi

    if (( attempt <= retries )); then sleep 1; continue; else break; fi
  done

  result=$(jq -n \
    --arg name "$name" \
    --arg url "$url" \
    --arg method "$method" \
    --arg http_code "$http_code" \
    --argjson success "$([[ "$success" == true ]] && echo true || echo false)" \
    --arg latency_ms "$latency_ms" \
    --arg bytes "$bytes" \
    --arg last_err "$last_err" \
    '{name:$name, url:$url, method:$method, http_code:($http_code|tonumber), success:$success, latency_ms:($latency_ms|tonumber), bytes:($bytes|tonumber), last_err:$last_err}')
  RESULTS+=("$result")

  if [[ "$success" == true ]]; then
    echo "[$idx/$TOTAL] $name -> OK (status $http_code, ${latency_ms}ms)"
  else
    echo "[$idx/$TOTAL] $name -> FAIL (status ${http_code:-n/a}, reason: $last_err)"
    if [[ -s "$temp_resp" ]]; then
      echo "  response snippet: $(head -n3 "$temp_resp" | sed -e 's/^/    /')"
    fi
  fi

  rm -f "$temp_resp"
done

printf '%s\n' "${RESULTS[@]}" | jq -s '.' > "$REPORT_FILE"
echo "Report saved to $REPORT_FILE"

if [[ -d "Logs" ]]; then
  echo "Scanning Logs/ for Error/Fatal entries..."
  latest_log=$(ls -1 Logs/app-log*.json 2>/dev/null | sort | tail -n1 || true)
  if [[ -n "$latest_log" ]]; then
    errors_found=$(jq -r 'select(.Level == "Error" or .Level == "Fatal" or .Level == "Critical") | .MessageTemplate' "$latest_log" 2>/dev/null || true)
    if [[ -n "$errors_found" ]]; then
      echo "ERRORS found in $latest_log:"
      jq -c 'select(.Level == "Error" or .Level == "Fatal" or .Level == "Critical")' "$latest_log" | sed -e 's/^/  /'
    else
      echo "No Error/Fatal entries in $latest_log."
    fi
  else
    echo "No Logs/app-log*.json files found."
  fi
else
  echo "Logs/ directory not present where script is running. Skipping log scan."
fi

TOTAL_DONE=$(jq 'length' "$REPORT_FILE")
FAILED=$(jq '[.[] | select(.success==false)] | length' "$REPORT_FILE")
OKS=$((TOTAL_DONE - FAILED))
echo "Summary: total=$TOTAL_DONE ok=$OKS failed=$FAILED"
if (( FAILED > 0 )); then
  jq -r '.[] | select(.success==false) | "\(.name) -> \(.last_err) (status:\(.http_code))"' "$REPORT_FILE" | sed -e 's/^/  - /'
  exit 1
else
  exit 0
fi
