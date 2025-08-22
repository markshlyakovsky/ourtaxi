using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Taxi_sheet.Model;

// Required using statements for the PDF generator
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Taxi_sheet.Pages
{
    public class ScheduledUserInput
    {
        public bool IsSelected { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string StartTime { get; set; }
        public string ReturnTime { get; set; }
    }

    public class IndexModel : PageModel
    {
        public List<Users> AllUsers { get; set; } = new List<Users>();

        [BindProperty]
        public DateTime ScheduleDate { get; set; } = DateTime.Today;

        // ** UPDATED SECTION: Use a Dictionary for time mappings **
        public Dictionary<string, string> TimeMappings { get; private set; }
        public List<string> AvailableReturnTimes { get; private set; }

        public IndexModel()
        {
            // The Dictionary now holds the direct relationship between start and return times.
            TimeMappings = new Dictionary<string, string>
            {
                { "08:45", "19:00" },
                { "09:00", "19:00" },
                { "10:30", "20:30" },
                { "10:45", "20:30" },
                { "12:30", "22:30" }
            };

            // This list still provides all unique options for the "Return Time" dropdown.
            AvailableReturnTimes = TimeMappings.Values.Distinct().OrderBy(t => t).ToList();
        }
        // ** END OF UPDATED SECTION **

        // The OnGet() method remains unchanged.
        public void OnGet()
        {
            Helper dbHelper = new Helper();
            DataTable usersDt = dbHelper.RetrieveTable("SELECT * FROM Users ORDER BY Name", "Users");
            foreach (DataRow row in usersDt.Rows)
            {
                AllUsers.Add(new Users
                {
                    Name = row["Name"].ToString(),
                    Address = row["Address"].ToString(),
                    Phone = row["Phone"].ToString()
                });
            }
        }

        // The OnPost() method remains unchanged and will work correctly.
        public IActionResult OnPost(List<ScheduledUserInput> schedule)
        {
            var selectedUsers = schedule
                .Where(u => u.IsSelected && (!string.IsNullOrWhiteSpace(u.StartTime) || !string.IsNullOrWhiteSpace(u.ReturnTime)))
                .ToList();

            var sortedSchedule = selectedUsers
                .OrderBy(u => TimeSpan.TryParse(u.StartTime, out var time) ? time : TimeSpan.MaxValue)
                .ToList();

            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .Text($"Taxi Schedule - {ScheduleDate:dd/MM/yyyy}")
                        .SemiBold().FontSize(22).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); columns.RelativeColumn(5);
                                columns.RelativeColumn(3); columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("Name");
                                header.Cell().Element(HeaderStyle).Text("Address");
                                header.Cell().Element(HeaderStyle).Text("Phone");
                                header.Cell().Element(HeaderStyle).Text("Start Time (הלוך)");
                                header.Cell().Element(HeaderStyle).Text("Return Time (חזור)");
                                static IContainer HeaderStyle(IContainer c) => c.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                            });

                            foreach (var user in sortedSchedule)
                            {
                                table.Cell().Element(CellStyle).Text(user.Name);
                                table.Cell().Element(CellStyle).Text(user.Address);
                                table.Cell().Element(CellStyle).Text(user.Phone);
                                table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(user.StartTime) ? "-" : user.StartTime);
                                table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(user.ReturnTime) ? "-" : user.ReturnTime);
                                static IContainer CellStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            byte[] pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"סידור מוניות לתאריך_{ScheduleDate:yyyy-MM-dd}.pdf");
        }
    }
}