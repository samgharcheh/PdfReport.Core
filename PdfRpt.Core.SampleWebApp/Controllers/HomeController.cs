﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using PdfRpt.Core.SampleWebApp.Reports;

namespace PdfRpt.Core.SampleWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        // only 1 thread can access the code
        private static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public HomeController(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            return await RunThrottledAsync(() =>
            {
                var reportBytes = InMemoryPdfReport.CreateInMemoryPdfReport(_hostingEnvironment.WebRootPath);
                return File(reportBytes, "application/pdf", "report.pdf");
            });
        }

        /// <summary>
        /// GET /home/streaming
        /// </summary>
        public async Task<IActionResult> Streaming()
        {
            return await RunThrottledAsync(() =>
            {
                var outputStream = new MemoryStream();
                InMemoryPdfReport.CreateStreamingPdfReport(_hostingEnvironment.WebRootPath, outputStream);
                return new FileStreamResult(outputStream, "application/pdf")
                {
                    FileDownloadName = "report.pdf"
                };
            });
        }

        public async Task<T> RunThrottledAsync<T>(Func<T> action)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                return action();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}