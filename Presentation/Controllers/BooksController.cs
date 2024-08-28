﻿using Microsoft.AspNetCore.Mvc;
using Services.Contracts;
using Microsoft.AspNetCore.JsonPatch;
using Entities.DataTransferObjects;
using Presentation.ActionFilters;
using Entities.RequestFeatures;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace Presentation.Controllers
{
    [ServiceFilter(typeof(LogFilterAttribute))]
    [ApiController]
    [Route("api/books")]
    [ApiExplorerSettings(GroupName = "v1")]
    public class BooksController : ControllerBase
    {
        private readonly IServiceManager _manager;

        public BooksController(IServiceManager manager)
        {
            _manager = manager;
        }

        [Authorize]
        [HttpHead]
        [HttpGet(Name = "GetAllBooksAsync")]
        [ServiceFilter(typeof(ValidateMediaTypeAttribute))]
        public async Task<IActionResult> GetAllBooksAsync([FromQuery] BookParameters bookParameters)
        {
            var linkParameters = new LinkParameters()
            {
                BookParameters = bookParameters,
                HttpContext = HttpContext
            };

            var result = await _manager.BookService.GetAllBooksAsync(linkParameters, false);
            Response.Headers["X-Pagination"] = JsonSerializer.Serialize(result.metaData);
            
            return result.linkResponse.HasLinks ? Ok(result.linkResponse.LinkedEntities) 
                : Ok(result.linkResponse.ShapedEntities);
        }


        [Authorize]
        [HttpGet("details")]
        public async Task<IActionResult> GetAllBooksWithDetailsAsync()
        {
            return Ok(await _manager.BookService.GetAllBooksWithDetailsAsync(false));
        }


        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOneBookAsync([FromRoute(Name = "id")] int id)
        {
            var book = await _manager.BookService.GetOneBookByIdAsync(id, false);
            return Ok(book);
        }


        [Authorize(Roles = "Editor, Admin")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        [HttpPost(Name = "CreateOneBookAsync")]
        public async Task<IActionResult> CreateOneBookAsync([FromBody] BookDtoForInsertion bookDto)
        {
            var book = await _manager.BookService.CreateOneBookAsync(bookDto);
            return StatusCode(201, book);
        }


        [Authorize(Roles = "Editor, Admin")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateOneBookAsync([FromRoute(Name = "id")] int id,
            [FromBody] BookDtoForUpdate bookDto)
        {
            await _manager.BookService.UpdateOneBookAsync(id, bookDto, false);
            return NoContent(); //204
        }


        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteOneBookAsync([FromRoute(Name = "id")] int id)
        {
            await _manager.BookService.DeleteOneBookAsync(id, false);
            return NoContent();   
        }


        [Authorize(Roles = "Editor, Admin")]
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> PartiallyUpdateOneBookAsync([FromRoute(Name = "id")] int id,
            [FromBody] JsonPatchDocument<BookDtoForUpdate> book)
        {
            if(book is null)
                return BadRequest(); //400
            
            //check entity
            var result = await _manager.BookService.GetOneBookForPatchAsync(id, false);

            book.ApplyTo(result.bookDtoForUpdate, ModelState);

            TryValidateModel(result.bookDtoForUpdate);

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            await _manager.BookService.SaveChangesForPatchAsync(result.bookDtoForUpdate, result.book);

            return NoContent(); //204
        }


        [Authorize]
        [HttpOptions]
        public IActionResult GetBookOptions()
        {
            Response.Headers.Add("Allow", "GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS");
            return NoContent();
        }
    }
}
