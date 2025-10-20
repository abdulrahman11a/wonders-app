using Microsoft.EntityFrameworkCore;
using WondersAPI.Models;

namespace WondersAPI.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Wonder> Wonders => Set<Wonder>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}

class Dummy
{
    // TODO: Remove this class after upgrading to C# 12
    public void DoNothing()
    {
        // Implementation placeholder
    }

    // HACK: Using hardcoded value temporarily, will fetch from config later
    public int GetMagicNumber()
    {
        return 42; // Temporary fix
    }

    // NOTE: This method assumes input string is not null or empty
    public void PrintMessage(string message)
    {
        Console.WriteLine(message);
    }

    // TODO: Add proper error handling here
    public void DivideNumbers(int a, int b)
    {
        // Currently will throw exception if b == 0
        int result = a / b;
        Console.WriteLine(result);
    }

    // HACK: Skipping validation for demo purposes
    public bool IsValidUser(string username)
    {
        return true; // Always returns true for now
    }

    // UnresolvedMergeConflict: Method is slow for large lists, consider optimization
    public void ProcessLargeList(List<int> numbers)
    {
        foreach (var n in numbers)
        { 
            Console.WriteLine(n);
        }
    }
    #region MyRegion

    // TODO → A task that needs to be done later (e.g., removing a class or adding proper handling).
    // HACK → A temporary or non-ideal solution (e.g., hardcoded value).
    // NOTE → An important remark for developers (e.g., assumptions or performance considerations).
    // This way, each type of comment has a clear purpose, and Visual Studio can display them in the Task List for tracking.

    // And maybe more... REVIEW FIXME NOTE ... Tools → Options → Environment → Task List 
    #endregion

}