using System.Text.RegularExpressions;

namespace HtmlSerializer
{
    /// <summary>
    /// השירות הראשי: טוען דף אינטרנט וממיר את ה-HTML שלו לעץ של אובייקטי HtmlElement.
    /// </summary>
    public class HtmlSerializerService
    {
        // HttpClient אחד משותף לכל הקריאות.
        // יצירת HttpClient חדש בכל קריאה עלולה לגרום למיצוי ה-sockets במערכת.
        private static readonly HttpClient _client = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient();
            // חלק מהאתרים חוסמים בקשות ללא User-Agent של דפדפן
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (HtmlSerializer)");
            return client;
        }

        // ---------------------------------------------------------------
        // שלב 1: קריאה לדף אינטרנט
        // ---------------------------------------------------------------

        /// <summary>מבצעת קריאת GET לכתובת ומחזירה את ה-HTML כמחרוזת.</summary>
        public async Task<string> Load(string url)
        {
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode(); // זריקת חריגה אם השרת החזיר שגיאה (4xx/5xx)
            var html = await response.Content.ReadAsStringAsync();
            return html;
        }

        /// <summary>טעינה וסריאליזציה בפעולה אחת.</summary>
        public async Task<HtmlElement> LoadAndSerialize(string url)
        {
            string html = await Load(url);
            return Serialize(html);
        }

        // ---------------------------------------------------------------
        // שלב 2: פירוק לפי תגיות
        // ---------------------------------------------------------------

        /// <summary>
        /// מפרקת את מחרוזת ה-HTML לרשימת חלקים: כל חלק הוא תגית שלמה (&lt;...&gt;) או קטע טקסט.
        /// </summary>
        public static List<string> Tokenize(string html)
        {
            // הסרת הערות HTML: <!-- ... -->
            html = Regex.Replace(html, @"<!--.*?-->", "", RegexOptions.Singleline);

            // ריקון התוכן של script ו-style (נשארות רק התגיות עצמן).
            // תוכן JavaScript/CSS עלול להכיל את התווים < ו-> ולשבש את הפירוק.
            html = Regex.Replace(html, @"(<(script|style)\b[^>]*>).*?(</\2\s*>)", "$1$3",
                                 RegexOptions.Singleline | RegexOptions.IgnoreCase);

            // ניקוי ירידות שורה ורווחים מיותרים: כל רצף של תווי רווח הופך לרווח יחיד
            html = Regex.Replace(html, @"\s+", " ");

            // פיצול לפי מבנה של תגית: מתחילה ב-< ומסתיימת ב->.
            // הסוגריים העגולים (קבוצת לכידה) גורמים ל-Regex.Split לשמור גם את התגיות עצמן בתוצאה.
            return Regex.Split(html, @"(<[^>]*>)")
                        .Select(s => s.Trim())
                        .Where(s => s.Length > 0) // הסרת מחרוזות ריקות
                        .ToList();
        }

        // ---------------------------------------------------------------
        // שלב 3: בניית העץ
        // ---------------------------------------------------------------

        /// <summary>
        /// בונה עץ HtmlElement מתוך מחרוזת HTML.
        /// הבנייה מתבצעת בלולאה (ולא ברקורסיה) כדי למנוע StackOverflow ב-HTML גדול.
        /// מוחזר אלמנט שורש וירטואלי בשם "document", שתגית ה-html היא בן שלו.
        /// </summary>
        public HtmlElement Serialize(string html)
        {
            HtmlHelper helper = HtmlHelper.Instance;
            List<string> tokens = Tokenize(html);

            // אובייקט השורש, ומשתנה שמחזיק את האלמנט הנוכחי בכל איטרציה
            var root = new HtmlElement { Name = "document" };
            HtmlElement current = root;

            foreach (string token in tokens)
            {
                // ---- קטע טקסט (לא תגית) ----
                // (מחרוזת שאינה בצורת <...>, כולל טקסט כמו "< 5" שמכיל את התו < בלבד)
                if (!(token.StartsWith('<') && token.EndsWith('>')))
                {
                    // טקסט פנימי של האלמנט הנוכחי.
                    // שרשור (ולא דריסה) - כי טקסט יכול להופיע בכמה מקטעים בין תגיות ילדים.
                    current.InnerHtml = string.IsNullOrEmpty(current.InnerHtml)
                        ? token
                        : current.InnerHtml + " " + token;
                    continue;
                }

                // ---- תגית ----
                // הסרת התווים < ו-> מהקצוות
                string content = token[1..^1].Trim();

                // התעלמות מתגיות מיוחדות כמו <!DOCTYPE html> או <?xml ...?>
                if (content.Length == 0 || content.StartsWith('!') || content.StartsWith('?'))
                    continue;

                // חיתוך המילה הראשונה (שם התגית)
                int spaceIndex = content.IndexOf(' ');
                string firstWord = spaceIndex == -1 ? content : content[..spaceIndex];
                string rest = spaceIndex == -1 ? string.Empty : content[(spaceIndex + 1)..];

                // מקרה של "<br/>" - הסלאש צמוד לשם התגית
                bool slashInName = firstWord.Length > 1 && firstWord.EndsWith('/');
                firstWord = firstWord.TrimEnd('/').ToLower();

                // 1) "/html" - הגענו לסוף ה-HTML
                if (firstWord == "/html")
                    break;

                // 2) תגית סוגרת - עלייה לרמה הקודמת בעץ
                if (firstWord.StartsWith('/'))
                {
                    string closingName = firstWord[1..];

                    // שיפור קטן על ההנחיה הבסיסית: מחפשים את האב הקרוב עם אותו שם.
                    // כך HTML לא תקין (תגית שנשכחה סגירתה) לא ישבש את כל העץ,
                    // ותגית סוגרת שאין לה תגית פותחת מתאימה פשוט נזנחת.
                    HtmlElement? opened = current;
                    while (opened != null && opened != root && opened.Name != closingName)
                        opened = opened.Parent;

                    if (opened != null && opened != root)
                        current = opened.Parent ?? root;

                    continue;
                }

                // 3) תגית פותחת של תגית HTML מוכרת - יצירת אלמנט חדש
                if (helper.IsHtmlTag(firstWord))
                {
                    var element = new HtmlElement
                    {
                        Name = firstWord,
                        Parent = current
                    };
                    current.Children.Add(element);

                    // פירוק המשך המחרוזת ל-Attributes
                    ParseAttributes(rest, element);

                    // בדיקה האם התגית סוגרת את עצמה:
                    // מסתיימת ב-"/" או נמצאת ברשימת התגיות שאינן דורשות סגירה
                    bool selfClosing = slashInName
                                       || content.EndsWith('/')
                                       || helper.IsVoidTag(firstWord);

                    // אם התגית אינה סוגרת את עצמה, ייתכן שיש לה תוכן פנימי,
                    // ולכן היא הופכת לאלמנט הנוכחי. אחרת - האלמנט הנוכחי נשאר כפי שהיה.
                    if (!selfClosing)
                        current = element;

                    continue;
                }

                // 4) תגית שאינה ברשימת תגיות ה-HTML (למשל תגיות SVG פנימיות או רכיבים מותאמים)
                // לא נוצר עבורה אלמנט, והיא אינה נכנסת ל-InnerHtml כטקסט.
            }

            return root;
        }

        /// <summary>
        /// מפרקת את מחרוזת ה-attributes של תגית ומעדכנת את האלמנט:
        /// Attributes, Id ו-Classes.
        /// </summary>
        private static void ParseAttributes(string attributesText, HtmlElement element)
        {
            // הביטוי מזהה:
            //   שם attribute (קבוצה 1)
            //   ואחריו, אופציונלית, = עם ערך בגרשיים כפולים (2), בגרש בודד (3) או ללא גרשיים (4).
            // כך נתמכים גם attributes ללא ערך, כגון disabled או hidden.
            var regex = new Regex(@"([^\s=/""']+)(?:\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s""'>]+)))?");

            foreach (Match match in regex.Matches(attributesText))
            {
                string name = match.Groups[1].Value.ToLower();
                string value = match.Groups[2].Success ? match.Groups[2].Value
                             : match.Groups[3].Success ? match.Groups[3].Value
                             : match.Groups[4].Success ? match.Groups[4].Value
                             : string.Empty;

                element.Attributes[name] = value;
            }

            // Id - מתוך ה-attributes, אם קיים
            if (element.Attributes.TryGetValue("id", out string? id))
                element.Id = id;

            // class - פירוק לפי רווחים, כי לאלמנט יכולים להיות כמה classes
            if (element.Attributes.TryGetValue("class", out string? classes))
                element.Classes = classes.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}
