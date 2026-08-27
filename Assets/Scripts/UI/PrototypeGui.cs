using UnityEngine;

namespace Ashbound
{
    public static class PrototypeGui
    {
        public static GUIStyle Title, Heading, Text, Small, Button, Center, CardTitle;
        public static void Initialize()
        {
            if (Title != null) return;
            Title = Style(40, Palette.Gold, FontStyle.Bold);
            Heading = Style(23, Color.white, FontStyle.Bold);
            Text = Style(16, new Color(.87f, .89f, .91f)); Text.wordWrap = true;
            Small = Style(12, new Color(.66f, .72f, .77f)); Small.wordWrap = true;
            Center = Style(18, Color.white); Center.alignment = TextAnchor.MiddleCenter; Center.wordWrap = true;
            CardTitle = Style(18, Palette.Gold, FontStyle.Bold); CardTitle.wordWrap = true;
            Button = new GUIStyle(GUI.skin.button) { fontSize = 15, padding = new RectOffset(12, 12, 9, 9), wordWrap = true };
        }
        private static GUIStyle Style(int size, Color color, FontStyle weight = FontStyle.Normal)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = weight };
            style.normal.textColor = color; return style;
        }
        public static void Box(Rect rect, Color color)
        { var old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old; }
        public static void Panel(Rect rect) { Box(rect, new Color(.045f, .06f, .08f, .96f)); Box(new Rect(rect.x, rect.y, rect.width, 2), new Color(.45f, .39f, .26f)); }
        public static bool Click(Rect rect, string label) => GUI.Button(rect, label, Button);
        public static void Label(float x, float y, float width, float height, string text, GUIStyle style = null) => GUI.Label(new Rect(x, y, width, height), text, style ?? Text);
        public static Matrix4x4 Scale()
        {
            Initialize(); var old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1)); return old;
        }
        public static void Bar(Rect rect, float fraction, Color color)
        { Box(rect, new Color(.16f, .19f, .23f)); Box(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), color); }
    }
}
