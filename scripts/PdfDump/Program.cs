using UglyToad.PdfPig;

var path = args.Length > 0 ? args[0] : @"C:\Users\T-GAMER\Downloads\Profile.pdf";
using var doc = PdfDocument.Open(path);
foreach (var page in doc.GetPages())
    Console.WriteLine("---PAGE---" + page.Text);
