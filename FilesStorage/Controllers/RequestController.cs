﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.StaticFiles;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FilesStorage.Controllers
{
    [Route("file_storage")]
    [ApiController]
    public class RequestController : ControllerBase
    {
        private readonly ILogger<RequestController> _logger;
        private readonly string PutHeaderCopyFrom = "Copy-File-From";

        public RequestController(ILogger<RequestController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{*path}")]
        public ActionResult GetFile(string path)
        {
            string fullPath = FileProcessing.GetFullPath(path);

            if (Directory.Exists(fullPath))
            {
                try
                {
                    List<string> response = new List<string>();
                    response = FileProcessing.ProcessDirectory(fullPath);
                    return new JsonResult(response);
                }
                catch
                {
                    return StatusCode(500);
                }
                
            }
            else if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    var provider = new FileExtensionContentTypeProvider();
                    if (!provider.TryGetContentType(fullPath, out var contentType))
                    {
                        contentType = "application/unknown";
                    }

                    FileStream fileStream = new FileStream(fullPath, FileMode.Open);
                    return File(fileStream, contentType, Path.GetFileName(fullPath));
                }
                catch
                {
                    return StatusCode(500);
                }
                
            }
            
            return NotFound();
        }


        [HttpDelete("{*path}")]
        public ActionResult DeleteFile(string path)
        {
            string fullPath;

            if (path == null)
            {
                // trying delete root directory 
                return BadRequest();
            }
            else
            {
                fullPath = FileProcessing.GetFullPath(path);
            }

            try
            {
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                    return Ok();
                }
                else if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    return Ok();
                }
            }
            catch
            {
                return StatusCode(500);
            }
            
            return NotFound();
        }

        [HttpHead("{*path}")]
        public ActionResult GetFileHeader(string path)
        {
            string fullPath = FileProcessing.GetFullPath(path);

            try
            {
                if (System.IO.File.Exists(fullPath))
                {
                    Dictionary<string, string> response = new Dictionary<string, string>();
                    response = FileProcessing.FileHeader(fullPath);
                    foreach(KeyValuePair<string, string> item in response)
                    {
                        Response.Headers.Add(item.Key, item.Value);
                    }

                    return Ok();
                }
            }
            catch
            {
                return StatusCode(500);
            }
            
            return NotFound();
        }

        [HttpPut("{*path}")]
        public ActionResult PutFile(IFormFile file, string path)
        {
            string fullPath = FileProcessing.GetFullPath(path);

            try
            {
                // copy file
                if (Request.Headers.ContainsKey(PutHeaderCopyFrom))
                {
                    FileProcessing.PutCodes resCode = FileProcessing.CopyFile(Request.Headers[PutHeaderCopyFrom], fullPath);
                    
                    if (resCode == FileProcessing.PutCodes.DoneCopy)
                    {
                        return Ok();
                    }
                    // trying delete root folder
                    else if (resCode == FileProcessing.PutCodes.BadRequest)
                    {
                        return BadRequest();
                    }
                    // directory or file doesnt exist
                    else
                    {
                        return NotFound();
                    }
                }
                // put file
                else if (Directory.Exists(fullPath))
                {
                    using (FileStream fs = new FileStream(Path.Combine(fullPath, file.FileName), FileMode.Create))
                    {
                        file.CopyTo(fs);
                    }
                    return Ok();
                }
            }
            catch
            {
                return StatusCode(500);
            }

            return NotFound();
        }
    }
}
