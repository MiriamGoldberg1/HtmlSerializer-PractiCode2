using HtmlSerializer;

// ===================================================================
// תוכנית הדגמה ובדיקה
// הרצה ללא פרמטרים: קריאה לכתובת https://netfree.link/shortcuts
// הרצה עם פרמטרים: dotnet run -- <url> "<selector>"
//   לדוגמה: dotnet run -- https://example.com "div p"
// ===================================================================

var service = new HtmlSerializerService();

string url = args.Length >= 1 ? args[0] : "https://netfree.link/shortcuts";
string query = args.Length >= 2 ? args[1] : "div";

Console.WriteLine($"Loading {url} ...");
HtmlElement root = await service.LoadAndSerialize(url);

// ---- הדפסת העץ (מוגבלת בעומק, באמצעות מחסנית - ללא ריקורסיה) ----
Console.WriteLine("\n=== Tree (first levels) ===");
PrintTree(root, maxDepth: 6, maxLines: 60);

// ---- תשאול לפי סלקטור ----
Selector selector = Selector.Parse(query);
var results = root.FindElements(selector).ToList();

Console.WriteLine($"\n=== Query: \"{selector}\" -> {results.Count} result(s) ===");
foreach (var el in results.Take(20))
{
    // הצגת האלמנט ושרשרת האבות שלו (באמצעות Ancestors)
    string path = string.Join(" > ", el.Ancestors().Reverse().Skip(1).Select(a => a.Name));
    Console.WriteLine($"{el}    [path: {path}]");
}

// ---- הדגמת פונקציות נוספות ----
Console.WriteLine($"\nTotal elements in tree: {root.Descendants().Count() - 1}");
Console.WriteLine($"HtmlHelper is a singleton: {ReferenceEquals(HtmlHelper.Instance, HtmlHelper.Instance)}");


// הדפסת עץ עם הזחה. נעשה שימוש ב-Stack כדי לא להיות תלויים בעומק ה-Call Stack.
static void PrintTree(HtmlElement root, int maxDepth, int maxLines)
{
    var stack = new Stack<(HtmlElement Element, int Depth)>();
    stack.Push((root, 0));
    int lines = 0;

    while (stack.Count > 0 && lines < maxLines)
    {
        var (element, depth) = stack.Pop();
        Console.WriteLine(new string(' ', depth * 2) + element);
        lines++;

        if (depth >= maxDepth) continue;

        // דחיפה בסדר הפוך כדי שהבן הראשון יודפס ראשון
        for (int i = element.Children.Count - 1; i >= 0; i--)
            stack.Push((element.Children[i], depth + 1));
    }
}
