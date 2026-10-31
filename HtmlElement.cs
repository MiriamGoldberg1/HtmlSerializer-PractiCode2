using System.Text;

namespace HtmlSerializer
{
    /// <summary>
    /// מייצגת תגית HTML אחת כצומת בעץ אובייקטים.
    /// </summary>
    public class HtmlElement
    {
        public string? Id { get; set; }

        /// <summary>שם התגית (div, p, span...).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// רשימת ה-Attributes של התגית: מפתח = שם ה-attribute, ערך = הערך שלו.
        /// נעשה שימוש ב-Dictionary כדי לאפשר שליפה מהירה לפי שם.
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>רשימת ה-classes (ה-attribute class מפורק לפי רווחים).</summary>
        public List<string> Classes { get; set; } = new List<string>();

        /// <summary>הטקסט הפנימי של האלמנט.</summary>
        public string InnerHtml { get; set; } = string.Empty;

        /// <summary>האלמנט האב בעץ (null עבור השורש).</summary>
        public HtmlElement? Parent { get; set; }

        /// <summary>האלמנטים הבנים בעץ.</summary>
        public List<HtmlElement> Children { get; set; } = new List<HtmlElement>();

        /// <summary>
        /// מחזירה רשימה שטוחה של האלמנט הנוכחי ושל כל צאצאיו מכל הדורות.
        /// ממומשת באמצעות Queue (סריקה לרוחב - BFS) ולא בריקורסיה,
        /// כדי למנוע StackOverflow בעצים גדולים.
        /// הערה: בהתאם להנחיות, האלמנט הנוכחי (this) הוא הראשון ברשימה.
        /// </summary>
        public IEnumerable<HtmlElement> Descendants()
        {
            var queue = new Queue<HtmlElement>();
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                // שליפת האובייקט שבראש התור
                HtmlElement current = queue.Dequeue();

                // yield return מחזיר את האיבר מיד, ללא בניית רשימה בזיכרון (הערכה עצלה)
                yield return current;

                // הוספת כל הבנים לסוף התור, לעיבוד באיטרציות הבאות
                foreach (HtmlElement child in current.Children)
                {
                    queue.Enqueue(child);
                }
            }
        }

        /// <summary>
        /// מחזירה רשימה שטוחה של כל אבות האלמנט, מהאב הישיר ועד השורש.
        /// אין צורך בתור או בריקורסיה: לכל אלמנט יש אב אחד בלבד, ולכן מספיק לטפס במעלה העץ.
        /// </summary>
        public IEnumerable<HtmlElement> Ancestors()
        {
            HtmlElement? current = Parent;
            while (current != null)
            {
                yield return current;
                current = current.Parent;
            }
        }

        /// <summary>
        /// בדיקה האם האלמנט עונה על כל התנאים של רמה אחת בסלקטור
        /// (שם תגית, id וכל ה-classes). תנאי שלא הוגדר בסלקטור אינו נבדק.
        /// </summary>
        public bool Matches(Selector selector)
        {
            if (!string.IsNullOrEmpty(selector.TagName) &&
                !string.Equals(Name, selector.TagName, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrEmpty(selector.Id) && Id != selector.Id)
                return false;

            // כל ה-classes שבסלקטור חייבים להופיע באלמנט
            if (selector.Classes.Any(c => !Classes.Contains(c)))
                return false;

            return true;
        }

        /// <summary>ייצוג טקסטואלי מקוצר של האלמנט, בסגנון תגית פותחת.</summary>
        public override string ToString()
        {
            var sb = new StringBuilder($"<{Name}");
            if (!string.IsNullOrEmpty(Id)) sb.Append($" id=\"{Id}\"");
            if (Classes.Count > 0) sb.Append($" class=\"{string.Join(" ", Classes)}\"");
            sb.Append('>');

            if (!string.IsNullOrWhiteSpace(InnerHtml))
            {
                string text = InnerHtml.Length > 40 ? InnerHtml[..40] + "..." : InnerHtml;
                sb.Append($" \"{text}\"");
            }
            return sb.ToString();
        }
    }
}
