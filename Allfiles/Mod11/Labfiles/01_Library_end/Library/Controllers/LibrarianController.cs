﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Library.Data;
using Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Library.Controllers
{
    public class LibrarianController : Controller
    {
        private LibraryContext _context;
        private IHostingEnvironment _environment;

        public LibrarianController(LibraryContext libraryContext, IHostingEnvironment environment)
        {
            _context = libraryContext;
            _environment = environment;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult GetBooks()
        {
            var booksQuery = from b in _context.Books
                             orderby b.Author
                             select b;

            return View(booksQuery);
        }

        [Authorize]
        public IActionResult GetBooksByGener()
        {
            var booksGenerQuery = from b in _context.Books
                                  orderby b.Genre.Name
                                  select b;

            return View(booksGenerQuery);
        }

        [Authorize]
        public IActionResult LendingBook(int id)
        {
            Book book = _context.Books.FirstOrDefault(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }
            PopulateBooksDropDownList(book.Genre.Id);
            return View(book);
        }

        [Authorize]
        [HttpPost, ActionName("LendingBooks")]
        public async Task<IActionResult> LendingBookPost(int id)
        {
            var bookToUpdate = _context.Books.FirstOrDefault(b => b.Id == id);
            bool isUpdated = await TryUpdateModelAsync<Book>(
                                bookToUpdate,
                                "",
                                c => c.Available == false);
            if (isUpdated)
            {
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            PopulateBooksDropDownList(bookToUpdate.Genre.Id);
            return View(bookToUpdate);
        }

        [Authorize]
        public IActionResult AddBook()
        {
            return View();
        }

        [Authorize]
        [HttpPost, ActionName("AddBook")]
        public IActionResult AddBookPost(Book book)
        {
            if (ModelState.IsValid)
            {
                if (book.PhotoAvatar != null && book.PhotoAvatar.Length > 0)
                {
                    book.ImageMimeType = book.PhotoAvatar.ContentType;
                    book.ImageName = Path.GetFileName(book.PhotoAvatar.FileName);
                    using (var memoryStream = new MemoryStream())
                    {
                        book.PhotoAvatar.CopyTo(memoryStream);
                        book.PhotoFile = memoryStream.ToArray();
                    }
                    book.Available = false;
                    _context.Add(book);
                    _context.SaveChanges();
                }
                return RedirectToAction(nameof(Index));
            }
            PopulateBooksDropDownList(book.Genre.Id);
            return View();
        }

        private void PopulateBooksDropDownList(int? selectedBook = null)
        {
            var books = from b in _context.Books
                        orderby b.Author
                        select b;

            ViewBag.BookID = new SelectList(books.AsNoTracking(), "Id", "Name", selectedBook);
        }

        public IActionResult GetImage(int id)
        {
            Book requestedBook = _context.Books.FirstOrDefault(b => b.Id == id);
            if (requestedBook != null)
            {
                string webRootpath = _environment.WebRootPath;
                string folderPath = "\\images\\";
                string fullPath = webRootpath + folderPath + requestedBook.ImageName;
                if (System.IO.File.Exists(fullPath))
                {
                    FileStream fileOnDisk = new FileStream(fullPath, FileMode.Open);
                    byte[] fileBytes;
                    using (BinaryReader br = new BinaryReader(fileOnDisk))
                    {
                        fileBytes = br.ReadBytes((int)fileOnDisk.Length);
                    }
                    return File(fileBytes, requestedBook.ImageMimeType);
                }
                else
                {
                    if (requestedBook.PhotoFile.Length > 0)
                    {
                        return File(requestedBook.PhotoFile, requestedBook.ImageMimeType);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }
            else
            {
                return NotFound();
            }
        }

    }
}