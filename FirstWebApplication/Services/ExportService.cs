using ClosedXML.Excel;
using FirstWebApplication.Entities;
using NetTopologySuite.IO;

namespace FirstWebApplication.Services
{
    public class ExportService
    {
        // Generate Excel workbook bytes for given obstacles
        public byte[] GenerateObstaclesExcel(IEnumerable<Obstacle> obstacles)
        {
            var wktReader = new WKTReader();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Obstacles");

            StyleHeaderRow(worksheet);
            CreateHeaderColumns(worksheet);

            int row = 2;
            foreach (var obstacle in obstacles)
            {
                PopulateObstacleRow(worksheet, row, obstacle, wktReader);

                if (row % 2 == 0)
                {
                    worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);
            worksheet.RangeUsed()?.SetAutoFilter();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private void StyleHeaderRow(IXLWorksheet worksheet)
        {
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private void CreateHeaderColumns(IXLWorksheet worksheet)
        {
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Name";
            worksheet.Cell(1, 3).Value = "Type";
            worksheet.Cell(1, 4).Value = "Height (m)";
            worksheet.Cell(1, 5).Value = "Description";
            worksheet.Cell(1, 6).Value = "Location (Lat, Lng)";
            worksheet.Cell(1, 7).Value = "Status";
            worksheet.Cell(1, 8).Value = "Registered By";
            worksheet.Cell(1, 9).Value = "Registered Date";
        }

        private void PopulateObstacleRow(IXLWorksheet worksheet, int row, Obstacle obstacle, WKTReader wktReader)
        {
            worksheet.Cell(row, 1).Value = obstacle.Id;
            worksheet.Cell(row, 2).Value = obstacle.Name ?? "N/A";
            worksheet.Cell(row, 3).Value = obstacle.ObstacleType?.Name ?? "N/A";
            worksheet.Cell(row, 4).Value = obstacle.Height;
            worksheet.Cell(row, 5).Value = obstacle.Description ?? "N/A";
            worksheet.Cell(row, 6).Value = ExtractLocationFromWkt(obstacle.Location, wktReader);
            worksheet.Cell(row, 7).Value = obstacle.CurrentStatus?.StatusType?.Name ?? "Unknown";
            worksheet.Cell(row, 8).Value = obstacle.RegisteredByUser?.Email ?? "Unknown";
            worksheet.Cell(row, 9).Value = obstacle.RegisteredDate.ToString("dd.MM.yyyy HH:mm");
        }

        private string ExtractLocationFromWkt(string? location, WKTReader wktReader)
        {
            if (string.IsNullOrEmpty(location))
                return "N/A";

            try
            {
                var geometry = wktReader.Read(location);
                if (geometry != null)
                {
                    var coord = geometry.Coordinate;
                    return $"{coord.Y:F4}, {coord.X:F4}";
                }
            }
            catch
            {
                return "Invalid";
            }

            return "N/A";
        }
    }
}
