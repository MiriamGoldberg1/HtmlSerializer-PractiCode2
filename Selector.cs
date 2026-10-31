using System.Text.RegularExpressions;

namespace HtmlSerializer
{
    /// <summary>
    /// מייצגת רמה אחת בשאילתת CSS Selector.
    /// הרמות מקושרות ברשימה (Parent/Child) - כל אב מכיל בן אחד בלבד (עץ בינארי מנוון).
    /// לדוגמה, השאילתה "div #mydiv .item" הופכת לשרשרת: div -> #mydiv -> .item
    /// </summary>
    public class Selector
    {
        public string? TagName { get; set; }
        public string? Id { get; set; }
        public List<string> Classes { get; set; } = new List<string>();

        public Selector? Parent { get; set; }
        public Selector? Child { get; set; }

        /// <summary>
        /// ממירה מחרוזת שאילתה לאובייקט Selector (השורש של השרשרת).
        /// </summary>
        public static Selector Parse(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty.", nameof(query));

            // פירוק לפי רווחים: כל חלק מייצג רמה נוספת (דור נוסף) בשאילתה
            string[] levels = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // אובייקט השורש, ומשתנה שמחזיק את הסלקטור הנוכחי בכל איטרציה
            var root = new Selector();
            Selector current = root;

            for (int i = 0; i < levels.Length; i++)
            {
                // פירוק הרמה לחלקים לפי המפרידים # ו-.
                // הביטוי תופס מפריד אופציונלי ואחריו רצף תווים שאינם מפרידים.
                // לדוגמה: "div#mydiv.a.b" -> "div", "#mydiv", ".a", ".b"
                foreach (Match part in Regex.Matches(levels[i], @"[#.]?[^#.]+"))
                {
                    string value = part.Value;

                    if (value.StartsWith('#'))
                    {
                        current.Id = value[1..];
                    }
                    else if (value.StartsWith('.'))
                    {
                        current.Classes.Add(value[1..]);
                    }
                    else if (HtmlHelper.Instance.IsHtmlTag(value))
                    {
                        // שם תגית נשמר רק אם הוא שם תקין של תגית HTML
                        current.TagName = value.ToLower();
                    }
                }

                // יצירת סלקטור חדש כבן של הנוכחי, רק אם יש רמה נוספת.
                // (כך לא נוצר סלקטור ריק בסוף השרשרת)
                if (i < levels.Length - 1)
                {
                    var child = new Selector { Parent = current };
                    current.Child = child;
                    current = child;
                }
            }

            return root;
        }

        public override string ToString()
        {
            string self = (TagName ?? "")
                        + (Id != null ? "#" + Id : "")
                        + string.Concat(Classes.Select(c => "." + c));
            return Child == null ? self : self + " " + Child;
        }
    }
}
