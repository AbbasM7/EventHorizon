using UnityEditor;
using UnityEngine;

public sealed class StarView : EditorWindow
{
    private static readonly Color BackgroundColor = new Color(0.03f, 0.05f, 0.1f);
    private static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.15f);
    private static readonly Color BorderColor = new Color(0.22f, 0.35f, 0.58f);
    private static readonly Color AccentColor = new Color(0.88f, 0.71f, 0.38f);
    private static readonly Color PrimaryTextColor = new Color(0.91f, 0.92f, 0.95f);
    private static readonly Color SecondaryTextColor = new Color(0.58f, 0.63f, 0.72f);
    private static readonly Color MutedTextColor = new Color(0.58f, 0.63f, 0.72f);
    private static readonly Color JokeTextColor = new Color(0.82f, 0.68f, 0.16f);
    private static readonly Color FooterCyan = new Color(0.34f, 0.66f, 0.66f);
    private static readonly Color FooterGreen = new Color(0.19f, 0.69f, 0.43f);
    private static readonly Color FooterYellow = new Color(0.82f, 0.68f, 0.16f);
    private static readonly Color FooterRed = new Color(0.78f, 0.15f, 0.18f);
    private static readonly Color[] AsciiPalette = { FooterCyan, FooterGreen, FooterYellow, FooterRed };

    private const string HeaderLabel = "--- STARVIEW ---";
    private const string WelcomeLine = "Welcome back FeLs!";
    private const string ProjectLine = "Project // Arrow Out";

    private const string CommandArt =
        @"          _|_
---@----(_)--@---
          | |
          ./ \.
";

    private const string FelinaArt =
        @"    .-----.   .-----.   .-----.
    | Fe  |   | Li  |   | Na  |
    '-----'   '-----'   '-----'
     BLOOD      METH     TEARS
";

    private static readonly string[] Quotes =
    {
        "All bad things come to an end.",
        "A bad day is only twenty-four hours.",
        "Every storm runs out of rain.",
        "There is a golden sky at the end of the storm.",
        "Keep going. Dawn is closer than it looks.",
        "The night does not win. Morning always shows up.",
        "Survive today. Tomorrow can argue later.",
        "Some chapters hurt. They still end.",
        "You are allowed to outlast the dark.",
        "Hope is stubborn. Let it be.",
        "Nothing heavy lasts forever in your hands.",
        "Rest if you need to. Do not quit the road.",
        "Even burnt skies make room for sunrise.",
        "The mountain is steep, not endless.",
        "You have made it through every hard day so far.",
        "The sea calms. So do people.",
        "Your current weather is not your climate.",
        "One quiet step still counts as progress.",
        "The wound is real. So is the healing.",
        "Not every ending is a loss.",
        "The clouds leave when they have said enough.",
        "Give it time. Time has teeth.",
        "The light returns in pieces. It still returns.",
        "Heavy seasons still change.",
        "What hurts now will not narrate your whole life.",
        "Breathe first. Solve second.",
        "Some victories are just refusing to fold.",
        "You do not need a miracle. You need the next hour.",
        "Even the loudest thunder has an ending.",
        "You are not behind. You are becoming.",
        "A dim day is not a dead future.",
        "Healing is ugly before it is visible.",
        "The sky breaks open for morning every single time.",
        "Peace can arrive quietly.",
        "The ending you fear is not the only ending available.",
        "Hold on long enough to meet the version of you that healed.",
        "The road bends. It does not always break.",
        "Better days do not ask permission. They arrive.",
        "You are still here. That matters.",
        "Hard times are loud. They are not permanent."
    };

    private static readonly string[] Jokes =
    {
        "I only know twenty-five letters of the alphabet. I do not know y.",
        "I used to hate facial hair, but then it grew on me.",
        "I am reading a book about anti-gravity. It is impossible to put down.",
        "I would tell you a construction joke, but I am still working on it.",
        "I stayed up all night to see where the sun went, and then it dawned on me.",
        "I used to be a baker, but I could not make enough dough.",
        "I told my computer I needed a break, and it said it would go to sleep.",
        "I used to be into archery, but I could never get the point.",
        "I told my suitcase there would be no vacations this year. Now I am dealing with emotional baggage.",
        "I used to be addicted to the hokey pokey, but I turned myself around.",
        "I am afraid for the calendar. Its days are numbered.",
        "I got hit in the head with a can of soda. Good thing it was a soft drink.",
        "I would avoid the sushi if I were you. It is a little fishy.",
        "I made a pencil with two erasers. It was pointless.",
        "I am friends with all the electricians. We have great current connections.",
        "I was wondering why the frisbee kept getting bigger. Then it hit me.",
        "The rotation of Earth really makes my day.",
        "I used to work in a shoe recycling shop. It was sole destroying.",
        "I do not trust stairs. They are always up to something.",
        "I used to be a banker, but I lost interest.",
        "I am on a seafood diet. I see food and I eat it.",
        "I once had a job at a calendar factory, but I got fired for taking a couple days off.",
        "I used to be a personal trainer, but I was not working out.",
        "I thought about going on an all-almond diet, but that is just nuts.",
        "I got a job at a mirror factory. I could really see myself working there.",
        "I asked my dog what is two minus two. He said nothing.",
        "I used to be a math teacher, but I lost count.",
        "I bought some shoes from a drug dealer. I do not know what he laced them with, but I was tripping all day.",
        "I tried to catch fog yesterday. Mist.",
        "I named my dog Five Miles so I can tell people I walk Five Miles every day.",
        "I have a fear of speed bumps, but I am slowly getting over it.",
        "I once got fired from a canned juice company. Apparently I could not concentrate.",
        "The shovel was a ground-breaking invention.",
        "A slice of apple pie is two dollars and fifty cents in Jamaica and three dollars in the Bahamas. Those are the pie rates of the Caribbean.",
        "I used to be a tap dancer until I fell in the sink.",
        "I was going to tell a time-travel joke, but you did not like it.",
        "The man who survived pepper spray and mustard gas is now a seasoned veteran.",
        "I got carded at a liquor store, and my Blockbuster card accidentally fell out. The cashier said never mind.",
        "I was going to tell a joke about boxing, but I forgot the punch line.",
        "I ordered a chicken and an egg online. I will let you know.",
        "I cannot believe I got fired from the keyboard factory. They told me I was not putting in enough shifts.",
        "I had a neck brace fitted years ago and I have never looked back since.",
        "Velcro is such a rip-off.",
        "I wanted to learn how to drive a stick shift, but I could not find the right gear.",
        "I used to run a dating service for chickens, but I was struggling to make hens meet.",
        "I can tell when people are being judgmental just by looking at them.",
        "I got a pet termite and named him Clint. Clint eats wood.",
        "I told my wife she should embrace her mistakes. She gave me a hug.",
        "I used to play piano by ear, but now I use my hands.",
        "I sold my vacuum yesterday. It was just collecting dust.",
        "I once got into so much debt that I could not even afford my electricity bills. They were the darkest times of my life.",
        "I am reading a horror story in braille. Something bad is about to happen. I can feel it.",
        "I used to work at a blanket factory, but it folded.",
        "I got a job drilling holes for water. It was well boring.",
        "I tried to write with a broken pencil, but it was pointless.",
        "I know a lot of jokes about retired people, but none of them work.",
        "I am no good at math, but I know that adding a bunch of dad jokes always multiplies the damage.",
        "I wanted to become a watchmaker, but I could not find the time.",
        "I accidentally swallowed some food coloring. The doctor says I am okay, but I feel like I have dyed a little inside.",
        "I used to be a librarian, but I got booked.",
        "I do not play soccer because I enjoy the sport. I am just doing it for kicks.",
        "I bought a boat because it was for sail.",
        "When does a joke become a dad joke? When it becomes apparent.",
        "I had a joke about paper, but it was tearable.",
        "I told a joke about a roof once. It went over everybody's head.",
        "I could not remember how to throw a boomerang, but then it came back to me.",
        "I used to be a scarecrow. I was outstanding in my field.",
        "I got a reversible jacket for my birthday. I cannot wait to see how it turns out.",
        "I used to be a transplant surgeon, but my heart was not in it.",
        "I got a job as a historian, but there was no future in it.",
        "I wanted to be a doctor, but I did not have the patience.",
        "I had a job at a bakery because I kneaded dough.",
        "I got a job at a coffee shop, but it was the same old grind.",
        "I used to install auto brakes, but it was too tiring.",
        "I tried to make a belt out of watches, but it was a waist of time.",
        "I am reading a book on helium. I just cannot put it down with a straight voice.",
        "I used to collect candy canes, but they were all in mint condition.",
        "I got a job at a paperless office. I quit because there was too much paperwork.",
        "I did a theatrical performance about puns. It was a play on words.",
        "I tried to organize a hide and seek tournament, but good players are really hard to find.",
        "I once worked in a factory that made fire hydrants. You could not park anywhere near the place.",
        "I used to make clocks, but it was too time consuming.",
        "I bought a thesaurus, but all the pages were blank. I have no words to describe how angry I am.",
        "I told my plants a joke. They have not stopped rooting for me since.",
        "The cemetery is so crowded. People are dying to get in.",
        "I was going to tell a joke about pizza, but it was too cheesy.",
        "I gave all my dead batteries away today. They were free of charge.",
        "I was struggling to figure out how lightning works, but then it struck me.",
        "I wanted to tell a joke about glue, but I could not stick with it.",
        "I tried to start a hot air balloon business, but it never really took off.",
        "I used to be a photographer, but I could not focus.",
        "I got a job as a tailor, but it was not suited for me.",
        "I was going to make myself a belt made of herbs, but that would be a waist of thyme.",
        "I got a job at a bicycle factory, but it was a two-tiring business.",
        "I told my friend ten jokes to make him laugh. Sadly, no pun in ten did.",
        "I used to sell security alarms door to door, but I was always alarming people.",
        "I had a job at a frozen food factory, but I just could not chill.",
        "I used to be a miner, but it was beneath me.",
        "I wanted to learn origami, but the experience was paper thin.",
        "I had a joke about elevators, but it works on many levels."
    };

    private bool _showFelina;
    private string _currentJoke;
    private string _currentQuote;

    [MenuItem("EventHorizon/Starview")]
    public static void Open()
    {
        StarView window = GetWindow<StarView>("StarView");
        window.minSize = new Vector2(720f, 320f);
    }

    private void OnEnable()
    {
        RefreshJoke();
    }

    private void OnGUI()
    {
        Font previousFont = GUI.skin.font;
        GUI.skin.font = EventHorizon.Editor.EventHorizonEditorFont.CascadiaMono;
        try
        {
            Rect fullRect = new Rect(0f, 0f, position.width, position.height);
            EditorGUI.DrawRect(fullRect, BackgroundColor);
            DrawTopography(fullRect);
            DrawStars(fullRect);

            Rect panelRect = new Rect(8f, 8f, position.width - 16f, position.height - 16f);
            DrawPanel(panelRect);

            GUILayout.BeginArea(new Rect(panelRect.x + 22f, panelRect.y + 22f, panelRect.width - 44f,
                panelRect.height - 44f));
            DrawHeader();
            EditorGUILayout.Space(18f);
            DrawCenteredContent(panelRect.width - 44f, panelRect.height - 44f);
            GUILayout.EndArea();
        }
        finally
        {
            GUI.skin.font = previousFont;
        }
    }

    private void DrawHeader()
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            richText = false,
            alignment = TextAnchor.MiddleCenter
        };
        DrawRainbowHeaderLine(HeaderLabel, headerStyle);
    }

    private void DrawCenteredContent(float availableWidth, float availableHeight)
    {
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = AccentColor;

        GUIStyle artStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        artStyle.normal.textColor = BorderColor;

        GUIStyle lineStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 13,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter
        };
        lineStyle.normal.textColor = MutedTextColor;

        GUIStyle accentStyle = new GUIStyle(lineStyle);
        accentStyle.normal.textColor = AccentColor;

        GUIStyle jokeStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        jokeStyle.normal.textColor = JokeTextColor;

        float contentWidth = Mathf.Clamp(availableWidth - 24f, 260f, 520f);

        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical(GUILayout.Width(contentWidth));

        GUILayout.Label(WelcomeLine, titleStyle);
        GUILayout.Space(10f);
        DrawQuoteBlock(contentWidth);
        GUILayout.Space(10f);
        DrawAsciiBlock(artStyle, contentWidth);
        GUILayout.Space(12f);
        GUILayout.Label(ProjectLine, accentStyle);
        GUILayout.Space(6f);
        float jokeHeight = Mathf.Max(20f, jokeStyle.CalcHeight(new GUIContent(_currentJoke), contentWidth));
        GUILayout.Label(_currentJoke, jokeStyle, GUILayout.Width(contentWidth), GUILayout.Height(jokeHeight));
        GUILayout.Space(12f);
        GUILayout.Label("Workspace // S:\\Game Dev\\Projects\\Arrow Out", lineStyle);
        GUILayout.Space(8f);
        GUILayout.Label("Framework // EventHorizon Logger Online", lineStyle);

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
    }

    private static void DrawPanel(Rect rect)
    {
        EditorGUI.DrawRect(rect, PanelColor);
        DrawTopography(rect, 0.45f);
        DrawStars(rect, 0.45f);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), AccentColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), AccentColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), AccentColor);
        EditorGUI.DrawRect(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), AccentColor);

        float footerY = rect.yMax - 6f;
        float segment = rect.width / 4f;
        EditorGUI.DrawRect(new Rect(rect.x, footerY, segment, 4f), FooterCyan);
        EditorGUI.DrawRect(new Rect(rect.x + segment, footerY, segment, 4f), FooterGreen);
        EditorGUI.DrawRect(new Rect(rect.x + segment * 2f, footerY, segment, 4f), FooterYellow);
        EditorGUI.DrawRect(new Rect(rect.x + segment * 3f, footerY, rect.width - segment * 3f, 4f), FooterRed);
    }

    private static void DrawRainbowHeaderLine(string text, GUIStyle style)
    {
        Rect rect = GUILayoutUtility.GetRect(10f, Mathf.Max(style.fontSize + 8f, 20f), GUILayout.ExpandWidth(true));
        GUIContent fullContent = new GUIContent(text);
        float totalWidth = style.CalcSize(fullContent).x;
        float x = rect.x + Mathf.Max(0f, (rect.width - totalWidth) * 0.5f);
        GUIContent content = new GUIContent();

        for (int i = 0; i < text.Length; i++)
        {
            content.text = text[i].ToString();
            Vector2 size = style.CalcSize(content);
            GUIStyle charStyle = new GUIStyle(style);
            charStyle.normal.textColor = AsciiPalette[i % AsciiPalette.Length];
            GUI.Label(new Rect(x, rect.y, size.x, rect.height), content, charStyle);
            x += size.x;
        }
    }

    private void DrawQuoteBlock(float contentWidth)
    {
        if (!_showFelina || string.IsNullOrWhiteSpace(_currentQuote))
        {
            return;
        }

        GUIStyle quoteStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        quoteStyle.normal.textColor = PrimaryTextColor;

        float quoteHeight = Mathf.Max(22f, quoteStyle.CalcHeight(new GUIContent(_currentQuote), contentWidth));
        GUILayout.Label(_currentQuote, quoteStyle, GUILayout.Width(contentWidth), GUILayout.Height(quoteHeight));
    }

    private void DrawAsciiBlock(GUIStyle artStyle, float contentWidth)
    {
        string[] lines = BuildAsciiLines();
        float lineHeight = Mathf.Max(artStyle.fontSize + 6f, 18f);
        float height = Mathf.Max(96f, lineHeight * lines.Length + 10f);
        Rect rect = GUILayoutUtility.GetRect(contentWidth, height, GUILayout.Width(contentWidth));

        for (int i = 0; i < lines.Length; i++)
        {
            GUIStyle lineStyle = new GUIStyle(artStyle);
            lineStyle.normal.textColor = _showFelina ? PrimaryTextColor : AsciiPalette[i % AsciiPalette.Length];
            Rect lineRect = new Rect(rect.x, rect.y + i * lineHeight, rect.width, lineHeight);
            GUI.Label(lineRect, lines[i], lineStyle);
        }

        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            _showFelina = !_showFelina;
            RefreshJoke();
            Event.current.Use();
            Repaint();
        }
    }

    private void RefreshJoke()
    {
        if (Jokes.Length == 0)
        {
            _currentJoke = string.Empty;
        }
        else
        {
            _currentJoke = Jokes[Random.Range(0, Jokes.Length)];
        }

        if (Quotes.Length == 0)
        {
            _currentQuote = string.Empty;
            return;
        }

        _currentQuote = Quotes[Random.Range(0, Quotes.Length)];
    }

    private string[] BuildAsciiLines()
    {
        if (!_showFelina)
        {
            return CommandArt.Split(new[] { '\n' }, System.StringSplitOptions.None);
        }

        return FelinaArt.Split(new[] { '\n' }, System.StringSplitOptions.None);
    }

    private static void DrawTopography(Rect rect, float alphaMultiplier = 1f)
    {
        Handles.BeginGUI();
        Color lineColor = new Color(1f, 1f, 1f, 0.09f * alphaMultiplier);
        for (int layer = 0; layer < 10; layer++)
        {
            Vector3[] points = new Vector3[48];
            float amplitude = 14f + layer * 3f;
            float y = rect.y + 32f + layer * 58f;
            for (int i = 0; i < points.Length; i++)
            {
                float x = rect.x + (rect.width / (points.Length - 1)) * i;
                float wave = Mathf.Sin((x * 0.015f) + layer * 0.85f) * amplitude;
                float drift = Mathf.Cos((x * 0.007f) + layer * 0.6f) * (amplitude * 0.45f);
                points[i] = new Vector3(x, y + wave + drift, 0f);
            }

            Handles.color = lineColor;
            Handles.DrawAAPolyLine(2f, points);
        }

        Handles.EndGUI();
    }

    private static void DrawStars(Rect rect, float alphaMultiplier = 1f)
    {
        Handles.BeginGUI();
        Color starColor = new Color(0.95f, 0.95f, 0.98f, 0.85f * alphaMultiplier);
        Vector2[] stars =
        {
            new Vector2(38f, 34f), new Vector2(92f, 60f), new Vector2(248f, 42f), new Vector2(332f, 88f),
            new Vector2(512f, 34f), new Vector2(648f, 78f), new Vector2(774f, 44f), new Vector2(884f, 110f),
            new Vector2(104f, 182f), new Vector2(214f, 236f), new Vector2(436f, 188f), new Vector2(586f, 248f),
            new Vector2(726f, 194f), new Vector2(848f, 286f), new Vector2(78f, 344f), new Vector2(304f, 388f),
            new Vector2(482f, 332f), new Vector2(690f, 402f), new Vector2(854f, 366f), new Vector2(932f, 458f)
        };

        for (int i = 0; i < stars.Length; i++)
        {
            Vector2 starPosition = rect.position + stars[i];
            if (starPosition.x > rect.xMax || starPosition.y > rect.yMax)
            {
                continue;
            }

            float size = i % 3 == 0 ? 3f : 2f;
            Handles.color = starColor;
            Handles.DrawSolidDisc(starPosition, Vector3.forward, size * 0.5f);

            if (i % 4 == 0)
            {
                Handles.color = new Color(0.95f, 0.95f, 0.98f, 0.28f * alphaMultiplier);
                Handles.DrawLine(starPosition + new Vector2(-4f, 0f), starPosition + new Vector2(4f, 0f));
                Handles.DrawLine(starPosition + new Vector2(0f, -4f), starPosition + new Vector2(0f, 4f));
            }
        }

        Handles.EndGUI();
    }
}