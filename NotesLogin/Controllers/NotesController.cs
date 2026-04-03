using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NotesLogin.Models;

using Microsoft.AspNetCore.Identity;

namespace NotesLogin.Controllers
{
    public class NotesController : Controller
    {
        private readonly NotesContext _context;

        private readonly UserManager<IdentityUser> _userManager;

        /* public NotesController(NotesContext context)
         {
             _context = context;
         }
        */

        public NotesController(NotesContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [AllowAnonymous]

        // GET: Notes
        /*  public async Task<IActionResult> Index()
          {
              return View(await _context.Notes.ToListAsync());
          }
        */

        /* public async Task<IActionResult> Index()
        {
            var notes = await _context.Notes.ToListAsync();

            // trying to get the username for the user instead of just showing the foreign key

            var users = _userManager.Users.ToList();

            var result = notes.Select(n => new
            {
                Note = n,
                UserName = users.FirstOrDefault(u => u.Id == n.NoteUserIdFk)?.UserName
            }).ToList();

            return View(result);
        }
        */

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id; // null if not logged in

            var notes = await _context.Notes.ToListAsync();

            // filter notes
            var filteredNotes = notes.Where(n =>
                n.NotePublic || (userId != null && n.NoteUserIdFk == userId)
            ).ToList();

            var users = _userManager.Users.ToList();

            var result = filteredNotes.Select(n => new
            {
                Note = n,
                UserName = users.FirstOrDefault(u => u.Id == n.NoteUserIdFk)?.UserName
            }).ToList();

            return View(result);
        }

        // GET: Notes/Details/5
        /*  public async Task<IActionResult> Details(int? id)
          {
              if (id == null)
              {
                  return NotFound();
              }

              var note = await _context.Notes
                  .FirstOrDefaultAsync(m => m.NoteId == id);
              if (note == null)
              {
                  return NotFound();
              }

              return View(note);
          }
        */

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var note = await _context.Notes.FirstOrDefaultAsync(m => m.NoteId == id);
            if (note == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            // block access to private notes
            if (!note.NotePublic && (user == null || note.NoteUserIdFk != user.Id))
            {
                return Forbid();
            }

            return View(note);
        }

        [Authorize]
        // GET: Notes/Create
        public IActionResult Create()
        {
            return View();
        }

        [Authorize] 
        // POST: Notes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("NoteId,NoteTitle,NoteText,NoteDate,NotePublic,NoteUserIdFk")] Note note)
        public async Task<IActionResult> Create([Bind("NoteId,NoteTitle,NoteText,NoteDate,NotePublic")] Note note)
        {
            if (ModelState.IsValid)
            {

                // get logged in user
                var user = await _userManager.GetUserAsync(User);

                // assign foreign key to that user
                note.NoteUserIdFk = user.Id;

                _context.Add(note);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(note);
        }

        // GET: Notes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var note = await _context.Notes.FindAsync(id);
            if (note == null)
            {
                return NotFound();
            }

            // only the user who created the note can edit it
            var user = await _userManager.GetUserAsync(User);

            if (user == null || note.NoteUserIdFk != user.Id)
                return Forbid();

            return View(note);
        }

        // POST: Notes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NoteId,NoteTitle,NoteText,NoteDate,NotePublic,NoteUserIdFk")] Note note)
        {
            if (id != note.NoteId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(note);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NoteExists(note.NoteId))
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

            var user = await _userManager.GetUserAsync(User);

            // block access to private notes
            if (!note.NotePublic && (user == null || note.NoteUserIdFk != user.Id))
            {
                return Forbid();
            }
            return View(note);
        }

        // GET: Notes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var note = await _context.Notes
                .FirstOrDefaultAsync(m => m.NoteId == id);
            if (note == null)
            {
                return NotFound();
            }

            // only the user who created the note can delete it
            var user = await _userManager.GetUserAsync(User);

            if (user == null || note.NoteUserIdFk != user.Id)
                return Forbid();


            return View(note);
        }

        // POST: Notes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {

            var note = await _context.Notes.FindAsync(id);
            if (note != null)
            {
                _context.Notes.Remove(note);
            }

            // block access to private notes

            var user = await _userManager.GetUserAsync(User);
           
            if (!note.NotePublic && (user == null || note.NoteUserIdFk != user.Id))
            {
                return Forbid();
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        private bool NoteExists(int id)
        {
            return _context.Notes.Any(e => e.NoteId == id);
        }
    }
}
