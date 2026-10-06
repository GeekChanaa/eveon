using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using ValidationException = VoltaXApi.Exceptions.ValidationException;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}
void Rejects(Action action, string name)
{
    try { action(); }
    catch (ValidationException) { Check(true, name); return; }
    throw new Exception("FAILED (accepted): " + name);
}

// ---- Dynamic LINQ: field names are resolved, values are parameters ----
var parent = new Parent { Name = "north" };
var items = new List<Item>
{
    new() { ID = 1, Name = "alpha", Count = 3, State = ItemState.Open, Parent = parent, Children = new() { new Child { Label = "red" } } },
    new() { ID = 2, Name = "beta", Count = 5, State = ItemState.Closed, Children = new() { new Child { Label = "blue" } } },
    new() { ID = 3, Name = "gamma", Count = 3, State = ItemState.Closed, Parent = parent, Children = new() },
}.AsQueryable();

List<Item> Run(GlobalParams p) => SafeDynamicQuery.Apply(items, p).ToList();

foreach (var malicious in new[] { "Id == 1 || true", "x\") || (1==1", "np(Name)", "Name.GetType()", "PasswordHash", "Parent.Secret",
                                  "Name; DROP TABLE Users", "Children.Label.Length", "Parent.Name.Length", "", "Missing" })
{
    Rejects(() => Run(new GlobalParams { FilterBy = new[] { malicious }, FilterValue = new[] { "1" } }), $"filter field '{malicious}' rejected");
    Rejects(() => Run(new GlobalParams { SearchBy = new[] { malicious }, SearchValue = "a" }), $"search field '{malicious}' rejected");
    if (malicious.Length > 0) Rejects(() => Run(new GlobalParams { OrderBy = malicious }), $"sort field '{malicious}' rejected");
}
Rejects(() => Run(new GlobalParams { OrderBy = "Name", ReverseOrder = "desc, Count" }), "sort direction other than asc/desc rejected");
Rejects(() => Run(new GlobalParams { FilterBy = new[] { "Name", "Count" }, FilterValue = new[] { "a", "3" }, FilterMethod = "|| true ||" }), "filter method other than ||/&& rejected");
Rejects(() => Run(new GlobalParams { FilterBy = new[] { "Name", "Count" }, FilterValue = new[] { "a" } }), "missing filter value rejected");
Rejects(() => Run(new GlobalParams { FilterBy = new[] { "Count" }, FilterValue = new[] { "3 || true" } }), "non-numeric value for numeric field rejected");

Check(Run(new GlobalParams { FilterBy = new[] { "Name" }, FilterValue = new[] { "x\") || (1==1" } }).Count == 0, "injection text as a filter value matches nothing");
Check(Run(new GlobalParams { SearchBy = new[] { "Name" }, SearchValue = "\") || true || (\"" }).Count == 0, "injection text as a search value matches nothing");
Check(Run(new GlobalParams { FilterBy = new[] { "count" }, FilterValue = new[] { "3" } }).Select(i => i.ID).SequenceEqual(new[] { 1, 3 }), "numeric filter, case-insensitive field name");
Check(Run(new GlobalParams { FilterBy = new[] { "State" }, FilterValue = new[] { "closed" } }).Count == 2, "enum filter by name");
Check(Run(new GlobalParams { FilterBy = new[] { "Name", "Count" }, FilterValue = new[] { "beta", "3" }, FilterMethod = "||" }).Count == 3, "|| combination");
Check(Run(new GlobalParams { FilterBy = new[] { "Name", "Count" }, FilterValue = new[] { "alpha", "3" }, FilterMethod = "&&" }).Count == 1, "&& combination");
Check(Run(new GlobalParams { FilterBy = new[] { "Children.Label" }, FilterValue = new[] { "blue" } }).Single().ID == 2, "collection navigation filter");
Check(Run(new GlobalParams { FilterBy = new[] { "Parent-Name" }, FilterValue = new[] { "north" } }).Count == 2, "reference navigation filter");
Check(Run(new GlobalParams { SearchBy = new[] { "Name", "Parent.Name" }, SearchValue = "orth" }).Count == 2, "search across own and navigation fields");
Check(Run(new GlobalParams { OrderBy = "count", ReverseOrder = "y" }).First().ID == 2, "descending sort");
Check(Run(new GlobalParams { OrderBy = "Name", ReverseOrder = "asc" }).First().Name == "alpha", "ascending sort");

// ---- Uploads: type from magic bytes, size limit ----
IFormFile File(byte[] bytes, string name) => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name);
var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };
Check(ImageUploadValidator.Validate(File(png, "a.html")).Extension == ".png", "PNG detected from content, client name ignored");
Check(ImageUploadValidator.Validate(File(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "x")).Extension == ".jpg", "JPEG detected");
Check(ImageUploadValidator.Validate(File(Encoding.ASCII.GetBytes("RIFF\0\0\0\0WEBPVP8 "), "x")).Extension == ".webp", "WebP detected");
Rejects(() => ImageUploadValidator.Validate(File(Encoding.UTF8.GetBytes("<svg onload=alert(1)>"), "logo.png")), "SVG disguised as .png rejected");
Rejects(() => ImageUploadValidator.Validate(File(Encoding.UTF8.GetBytes("<html><script>"), "a.jpg")), "HTML rejected");
Rejects(() => ImageUploadValidator.Validate(File(png, "a.png"), maxBytes: 4), "oversized file rejected");
Rejects(() => ImageUploadValidator.Validate(File(Array.Empty<byte>(), "a.png")), "empty file rejected");

// ---- Generic update: only allowed scalar fields are copied ----
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
var card = new Card { ID = 7, UserID = 1, Balance = 10, CardNumber = "VX-1", Note = "old", Status = CardStatusEnum.Active };
var body = JsonDocument.Parse("""{"id":99,"userID":2,"balance":1000000,"cardNumber":"VX-2","isDeleted":true,"note":"new","user":{"id":2}}""").RootElement;
var copied = EntityUpdate.Apply(card, body, options);
Check(card.ID == 7 && card.UserID == 1 && card.Balance == 10 && card.CardNumber == "VX-1" && !card.IsDeleted, "id, owner, balance, [NotUpdatable] and audit fields ignored");
Check(card.Note == "new" && copied.SequenceEqual(new[] { "Note" }), "allowed scalar field copied");
Rejects(() => EntityUpdate.Apply(card, JsonDocument.Parse("""{"note":{"x":1}}""").RootElement, options), "wrongly typed value rejected");

// ---- Card data: no PAN, no CVV ----
Check(typeof(DebitCard).GetProperties().All(p => p.Name is not ("CardNumber" or "CVV")), "DebitCard model holds no PAN or CVV");
var webOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
try
{
    JsonSerializer.Deserialize<AddDebitCardDto>("""{"providerToken":"devtok_abcdefabcdefabcdef","last4":"4242","expiryMonth":1,"expiryYear":2099,"cardNumber":"4242424242424242","cvv":"123"}""", webOptions);
    throw new Exception("FAILED: payload with cardNumber/cvv accepted");
}
catch (JsonException) { Check(true, "payload with cardNumber/cvv fields rejected"); }

bool Valid(AddDebitCardDto dto) => Validator.TryValidateObject(dto, new ValidationContext(dto), new List<ValidationResult>(), validateAllProperties: true);
var ok = new AddDebitCardDto { ProviderToken = "devtok_abcdefabcdefabcdef", Brand = DebitCardTypeEnum.Visa, Last4 = "4242", ExpiryMonth = 12, ExpiryYear = 2099 };
Check(Valid(ok), "token + display metadata accepted");
Check(!Valid(new AddDebitCardDto { ProviderToken = "4242424242424242", Last4 = "4242", ExpiryMonth = 12, ExpiryYear = 2099 }), "PAN passed as token rejected");
Check(!Valid(new AddDebitCardDto { ProviderToken = "devtok_abcdefabcdefabcdef", Name = "4242 4242 4242 4242", Last4 = "4242", ExpiryMonth = 12, ExpiryYear = 2099 }), "PAN in holder name rejected");
Check(!Valid(new AddDebitCardDto { ProviderToken = "devtok_abcdefabcdefabcdef", Last4 = "42424", ExpiryMonth = 12, ExpiryYear = 2099 }), "more than four digits rejected");

Console.WriteLine($"{checks} checks passed");

enum ItemState { Open, Closed }
class Parent { public string? Name { get; set; } public string? Secret { get; set; } }
class Child { public string? Label { get; set; } }
class Item
{
    public int ID { get; set; }
    public string? Name { get; set; }
    public int Count { get; set; }
    public ItemState State { get; set; }
    public string? PasswordHash { get; set; }
    public Parent? Parent { get; set; }
    public List<Child> Children { get; set; } = new();
}
