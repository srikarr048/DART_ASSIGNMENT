using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Globalization;

namespace MarsImageDownloader.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MarsRoverPhotoController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public MarsRoverPhotoController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://images-api.nasa.gov/");
        }

        [HttpGet]
        public async Task<IActionResult> GetPhotos()
        {
            string[] dates = System.IO.File.ReadAllLines("dates.txt");
            List<string> photoUrls = new List<string>();
            var directoryPath = System.IO.Directory.GetCurrentDirectory();

            foreach (string date in dates)
            {
                var formattedDate = FormatDate(date);
                var apiUrl = $"/search?q={formattedDate}&media_type=image";

                using (HttpResponseMessage response = await _httpClient.GetAsync(apiUrl))
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic json = JsonConvert.DeserializeObject(responseBody);

                    foreach (var item in json.collection.items)
                    {
                        string imageUrl = item.links[0].href; // Assuming the image URL is in the "links" array
                        var guid = Guid.NewGuid();
                        await DownloadImage(imageUrl, $"{formattedDate}_{guid}.jpg");
                        photoUrls.Add($"{formattedDate}_{guid}.jpg");
                    }
                }
            }

            return Ok(photoUrls);
        }

        private async Task DownloadImage(string url, string fileName)
        {
            // Download the image from the provided URL and save it using the provided file name
            byte[] imageBytes = await _httpClient.GetByteArrayAsync(url);
            await System.IO.File.WriteAllBytesAsync($"Images/{fileName}", imageBytes); // Saving images to the "Images" directory
        }

        private string FormatDate(string date)
        {
            string[] formats = { "MM/dd/yy", "MMMM d, yyyy", "MMM-dd-yyyy", "MMMMM dd, yyyy" };
            DateTime parsedDate;

            if (DateTime.TryParseExact(date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
            {
                return parsedDate.ToString("yyyy-MM-dd");
            }
            else
            {
                // Handle invalid date format
                throw new FormatException("Invalid date format: " + date);
            }
        }
    }
}
