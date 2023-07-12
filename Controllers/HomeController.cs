using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContosoTeamStats.Data;
using StackExchange.Redis;
using System.Diagnostics;

namespace ContosoTeamStats.Controllers
{
    public class HomeController : Controller
    {
        private readonly TeamContext _context;
        private readonly ICacheService _cache;

        public HomeController(TeamContext context, ICacheService cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: Teams
        public async Task<ActionResult> Index(string actionType, string resultType)
        {
            List<Team> teams = null;

            switch (actionType)
            {
                case "playGames": // Play a new season of games.
                    await PlayGames();
                    break;

                case "clearCache": // Clear the results from the cache.
                    await InvalidateCacheAsync();
                    break;

                case "rebuildDB": // Rebuild the database with sample data.
                    await RebuildDB();
                    break;
            }

            // Measure the time it takes to retrieve the results.
            Stopwatch sw = Stopwatch.StartNew();

            switch (resultType)
            {
                case "teamsSortedSet": // Retrieve teams from sorted set.
                    teams = await GetFromSortedSet();
                    break;

                case "teamsSortedSetTop5": // Retrieve the top 5 teams from the sorted set.
                    teams = await GetFromSortedSetTop5();
                    break;

                case "teamsList": // Retrieve teams from the cached List<Team>.
                    teams = await GetFromList();
                    break;

                case "fromDB": // Retrieve results from the database.
                default:
                    teams = GetFromDB();
                    break;
            }

            sw.Stop();
            double ms = sw.ElapsedTicks / (Stopwatch.Frequency / (1000.0));

            // Add the elapsed time of the operation to the ViewBag.msg.
            ViewBag.msg += " MS: " + ms.ToString();

            return View(teams);
        }

        // GET: Teams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Team == null)
            {
                return NotFound();
            }

            var team = await _context.Team
                .FirstOrDefaultAsync(m => m.ID == id);
            if (team == null)
            {
                return NotFound();
            }

            return View(team);
        }

        // GET: Teams/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Teams/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind("ID,Name,Wins,Losses,Ties")] Team team)
        {
            if (ModelState.IsValid)
            {
                await _context.Team.FindAsync(team);
                await _context.SaveChangesAsync();
                await InvalidateCacheAsync();
                return RedirectToAction("Index");
            }

            return View(team);
        }

        // GET: Teams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Team == null)
            {
                return NotFound();
            }

            var team = await _context.Team.FindAsync(id);
            if (team == null)
            {
                return NotFound();
            }
            return View(team);
        }

        // POST: Teams/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Wins,Losses,Ties")] Team team)
        {
            if (id != team.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(team);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeamExists(team.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(team);
        }

        // POST: Teams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Team == null)
            {
                return Problem("Entity set 'ContosoTeamStatsContext.Team'  is null.");
            }
            Team team = await _context.Team.FindAsync(id);
            _context.Team.Remove(team);
            await _context.SaveChangesAsync();
            await InvalidateCacheAsync();
            return RedirectToAction("Index");
        }

        private bool TeamExists(int id)
        {
          return _context.Team.Any(e => e.ID == id);
        }

        async Task PlayGames()
        {
            ViewBag.msg += "Updating team statistics. ";
            // Play a "season" of games.
            var teams = from t in _context.Team
                        select t;

            Team.PlayGames(teams);

            _context.SaveChanges();

            await InvalidateCacheAsync();
        }

        async Task RebuildDB()
        {
            ViewBag.msg += "Rebuilding DB. ";
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
            await InvalidateCacheAsync();
        }

        async Task InvalidateCacheAsync()
        {
            await _cache.RemoveByKeyMatchAsync("team*");
            ViewBag.msg += "Team data removed from cache. ";
        }

        List<Team> GetFromDB()
        {
            ViewBag.msg += "Results read from DB. ";
            var results = from t in _context.Team
                          orderby t.Wins descending
                          select t;

            return results.ToList<Team>();
        }

        async Task<List<Team>> GetFromSortedSet()
        {
            // If the key teamsSortedSet is not present, this method returns a 0 length collection.
            var teamsSortedSet = await _cache.SortedSetRangeByRankWithScoresAsync<Team>("teamsSortedSet");
            if (teamsSortedSet != null && teamsSortedSet.Any())
            {
                ViewBag.msg += "Reading sorted set from cache. ";
            }
            else
            {
                ViewBag.msg += "Teams sorted set cache miss. ";

                teamsSortedSet = GetFromDB();

                ViewBag.msg += "Storing results to cache. ";
                foreach (var t in teamsSortedSet)
                {
                    Console.WriteLine("Adding to sorted set: {0} - {1}", t.Name, t.Wins);
                    await _cache.SortedSetAddAsync("teamsSortedSet", t, t.Wins);
                }
            }
            return teamsSortedSet.ToList();
        }

        async Task<List<Team>> GetFromSortedSetTop5()
        {
            // If the key teamsSortedSet is not present, this method returns a 0 length collection.
            var teamsSortedSet = await _cache.SortedSetRangeByRankWithScoresAsync<Team>("teamsSortedSet", stop: 4, order: Order.Descending);
            if (teamsSortedSet.Count() == 0)
            {
                // Load the entire sorted set into the cache.
                await GetFromSortedSet();

                // Retrieve the top 5 teams.
                teamsSortedSet = await _cache.SortedSetRangeByRankWithScoresAsync<Team>("teamsSortedSet", stop: 4, order: Order.Descending);
            }

            ViewBag.msg += "Retrieving top 5 teams from cache. ";

            return teamsSortedSet.ToList();
        }

        async Task<List<Team>> GetFromList()
        {
            var teams = await _cache.GetByKeyMatchAsync<Team>("team*");

            if (teams != null && teams.Any())
            {
                ViewBag.msg += "List read from cache. ";
            }
            else
            {
                ViewBag.msg += "Teams list cache miss. ";
  
                teams = GetFromDB();

                ViewBag.msg += "Storing results to cache. ";

                foreach (var team in teams)
                {
                    await _cache.AddOrUpdateAsync($"team-{team.ID}", team, TimeSpan.FromMinutes(5));
                }
            }
            return teams.ToList();
        }


    }
}
