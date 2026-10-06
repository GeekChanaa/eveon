using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using VoltaXApi.Models;

public class InvoiceDocument : IDocument
{
    private string _logoImage;
    private readonly InvoiceData _data;
    private readonly CompanyInformations _companyInformations;
    private readonly IWebHostEnvironment _webHostEnvironment;
    float infoFontSize = 12f;              // Increased font size (default is often 10 or 11)
    Color valueColor = Colors.Grey.Darken2;  // A dark grey, lighter than black
    float rowBottomPadding = 5f;        

    public InvoiceDocument(
        InvoiceData data,
        CompanyInformations companyInformations,
         IWebHostEnvironment webHostEnvironment)
    {
        _data = data;
        _companyInformations = companyInformations;
        _webHostEnvironment = webHostEnvironment;
        _logoImage = Path.Combine(_webHostEnvironment.WebRootPath, "Assets", "images", "logo.jpg");
    }

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Montserrat"));
                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

       // Space below each info row item

    // --- Helper Method for creating styled info rows ---
    // (Place this method where it's accessible, e.g., in the same class or a static helper class)
    void AddInfoRow(ColumnDescriptor column, string label, string? value) // Added nullable 'string?' for value
    {
        column.Item()
            .PaddingBottom(rowBottomPadding) // Adds space AFTER this row item
            .Row(row =>
            {
                // Optional: Add horizontal spacing between the label and value if needed
                // row.Spacing(5);

                // Define relative widths: Label takes 1 part, Value takes ~2 parts
                row.RelativeItem(1) // Column for Label
                .Text(label)
                .FontSize(infoFontSize) // Apply font size
                .Bold();              // Keep label bold

                row.RelativeItem(2) // Column for Value (give it more space)
                .Text(value ?? string.Empty) // Use the value, handle if it's null
                .FontSize(infoFontSize)      // Apply font size
                .FontColor(valueColor);     // Apply the lighter color
            });
    }

    void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Width(100).Height(100).Image(QuestPDF.Infrastructure.Image.FromFile(_logoImage)); 
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text("INVOICE").Bold().FontSize(30);
                col.Item().Text($"#{_data.InvoiceNumber}").FontSize(16);
            });
        });
    }

    void ComposeContent(IContainer container)
    {
        container.PaddingTop(30).Column(col =>
        {
            AddInfoRow(col, "BILLED TO:", _data.BilledTo);
            AddInfoRow(col, "CARD NUMBER:", _data.CardNumber);
            AddInfoRow(col, "DATE:", _data.Date);


            var headerBgColor = Colors.Grey.Lighten2; 
            var borderColor = Colors.Grey.Medium;     
            var cellPadding = 5;                     
            var headerTextStyle = TextStyle.Default.Bold();

            col.Item().PaddingTop(20).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });



                // ----- HEADER -----
                table.Header(header =>
                {
                    header.Cell()
                            .Background(headerBgColor)
                            .BorderBottom(1) // Add a border below the header
                            .BorderColor(borderColor)
                            .Padding(cellPadding) // Add padding
                            .AlignLeft() // Or Left-align if preferred
                            .Text("DESCRIPTION").Style(headerTextStyle);

                    // Helper action to style header cells consistently
                    Action<string> HeaderCell = (text) =>
                    {
                        header.Cell()
                            .Background(headerBgColor)
                            .BorderBottom(1) // Add a border below the header
                            .BorderColor(borderColor)
                            .Padding(cellPadding) // Add padding
                            .AlignCenter() // Center-align header text
                            .Text(text).Style(headerTextStyle);
                    };

                    // Apply the style to each header cell
                    HeaderCell("Amount HT");
                    HeaderCell("VAT");
                    HeaderCell("Total Amount");
                });

                var dataRowBgColor = Colors.White; 

                
                // Description Cell
                table.Cell()
                    .Background(dataRowBgColor) 
                    .BorderBottom(1)            
                    .BorderColor(borderColor)   
                    .Padding(cellPadding)       
                    .AlignLeft()                
                    .Text("Card Recharge");     
                

                // Amount HT Cell
                table.Cell()
                    .Background(dataRowBgColor)
                    .BorderBottom(1)
                    .BorderColor(borderColor)
                    .Padding(cellPadding)
                    .AlignCenter()
                    .Text($"${_data.AmountHT:N2}");

                // VAT Cell
                table.Cell()
                    .Background(dataRowBgColor)
                    .BorderBottom(1)
                    .BorderColor(borderColor)
                    .Padding(cellPadding)
                    .AlignCenter()
                    .Text($"${_data.VAT:N2}");

                // Total Amount Cell
                table.Cell()
                    .Background(dataRowBgColor)
                    .BorderBottom(1)
                    .BorderColor(borderColor)
                    .Padding(cellPadding)
                    .AlignCenter() 
                    .Text($"${_data.TotalAmount:N2}"); 
            });
        });
    }

    void ComposeFooter(IContainer container)
{
    // --- Define Styles for consistency ---
    var footerFontSize = 9f; // Use a slightly smaller font size for footers
    var addressStyle = TextStyle.Default.FontSize(footerFontSize);
    var companyNameStyle = TextStyle.Default.SemiBold().FontSize(footerFontSize + 1); // Slightly larger/bolder name
    var labelStyle = TextStyle.Default.Bold().FontSize(footerFontSize); // Bold labels (RC, TP, etc.)
    var valueStyle = TextStyle.Default.FontSize(footerFontSize); // Regular weight values

    container
        .PaddingTop(20) // Adjust overall top padding for the footer section
        .Column(col =>
        {
            // Spacing between the elements in the main footer column (Name, Address, ID Row)
            col.Spacing(5); // Adjust vertical spacing between lines

            // 1. Company Name (Optional, if you have it in _companyInformations)
            // Centered alignment often looks good for the company name in a footer
            if (!string.IsNullOrEmpty(_companyInformations.CompanyName)) // Only show if available
            {
                col.Item()
                   .AlignCenter()
                   .Text(_companyInformations.CompanyName)
                   .Style(companyNameStyle);
            }

            // 2. Address Line
            // The image shows address centered, you can change to .AlignLeft() if preferred
            col.Item()
               .AlignCenter() // Or .AlignLeft()
               // Note: The image doesn't bold "Headquarters:"
               .Text($"Headquarters: {_companyInformations.Address ?? string.Empty}")
               .Style(addressStyle); // Apply consistent address style

            // 3. Identifiers Row (RC, TP, IF, CNSS, ICE)
            col.Item()
                .PaddingTop(5) // Add a little extra space above this specific line
                .AlignCenter() // Center the entire Row container
                .Row(row =>
                {
                    // Horizontal spacing between each identifier block (e.g., between RC block and TP block)
                    row.Spacing(15); // Adjust spacing for visual balance

                    // Helper Action to create styled Identifier: Value pairs
                    // This avoids repeating the .Text(text => ...) structure
                    Action<string, string?> AddIdentifier = (label, value) =>
                    {
                        // AutoItem sizes itself based on content
                        row.AutoItem()
                           .Text(text => // Use Text composition to mix styles
                           {
                               // Bold Label + colon + space
                               text.Span($"{label}: ").Style(labelStyle);
                               // Regular Value
                               text.Span(value ?? string.Empty).Style(valueStyle);
                           });
                    };

                    // Add each identifier using the helper
                    AddIdentifier("RC", _companyInformations.RC);
                    AddIdentifier("TP", _companyInformations.TP);
                    AddIdentifier("IF", _companyInformations.IF);
                    AddIdentifier("CNSS", _companyInformations.CNSS);
                    AddIdentifier("ICE", _companyInformations.ICE);
                });
        });
}

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
}