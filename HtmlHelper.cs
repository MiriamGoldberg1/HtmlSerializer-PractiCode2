using System.Text.Json;

namespace HtmlSerializer
{
    /// <summary>
    /// מחלקת עזר שמספקת את רשימות תגיות ה-HTML.
    /// ממומשת בתבנית Singleton: קיים מופע אחד בלבד לכל אורך חיי התוכנית,
    /// כך שקבצי ה-JSON נטענים פעם אחת בלבד ולא בכל שימוש.
    /// </summary>
    public sealed class HtmlHelper
    {
        // המופע היחיד של המחלקה.
        // אתחול סטטי מתבצע פעם אחת בלבד ע"י ה-CLR ובאופן בטוח בריבוי תהליכונים (thread-safe).
        private static readonly HtmlHelper _instance = new HtmlHelper();

        /// <summary>נקודת הגישה היחידה למופע.</summary>
        public static HtmlHelper Instance => _instance;

        /// <summary>כל התגיות הקיימות ב-HTML.</summary>
        public string[] HtmlTags { get; private set; }

        /// <summary>תגיות שאינן דורשות תגית סגירה (למשל br, img, input).</summary>
        public string[] HtmlVoidTags { get; private set; }

        // Constructor פרטי - מונע יצירת מופעים נוספים מחוץ למחלקה (ליבת ה-Singleton)
        private HtmlHelper()
        {
            // הקבצים מועתקים לתיקיית הפלט (ראו הגדרה ב-csproj),
            // ולכן הנתיב נבנה יחסית לתיקייה שממנה רצה התוכנית
            string basePath = Path.Combine(AppContext.BaseDirectory, "JSON");

            // קריאת תוכן הקובץ באמצעות מחלקת File
            string allTagsJson = File.ReadAllText(Path.Combine(basePath, "HtmlTags.json"));
            string voidTagsJson = File.ReadAllText(Path.Combine(basePath, "HtmlVoidTags.json"));

            // המרת ה-JSON למערך מחרוזות באמצעות JsonSerializer.Deserialize
            HtmlTags = JsonSerializer.Deserialize<string[]>(allTagsJson) ?? Array.Empty<string>();
            HtmlVoidTags = JsonSerializer.Deserialize<string[]>(voidTagsJson) ?? Array.Empty<string>();
        }

        /// <summary>בדיקה האם מחרוזת היא שם תקין של תגית HTML (ללא תלות באותיות גדולות/קטנות).</summary>
        public bool IsHtmlTag(string name) =>
            HtmlTags.Contains(name, StringComparer.OrdinalIgnoreCase);

        /// <summary>בדיקה האם התגית אינה דורשת תגית סגירה.</summary>
        public bool IsVoidTag(string name) =>
            HtmlVoidTags.Contains(name, StringComparer.OrdinalIgnoreCase);
    }
}
