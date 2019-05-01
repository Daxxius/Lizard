using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lizard
{
	public static class LizardTitleScreen
	{
		private struct MenuOption
		{
			public System.Action<TitleScreen> action;
			public string uiRef;
			public RectTransform rectTransform;
			public Text text;
			public Button button;
			public EventTrigger eventTrigger;
		}

		private static LinkedList<MenuOption> MenuOptions = new LinkedList<MenuOption>();

		private static Dictionary<string, LinkedListNode<MenuOption>> MenuOptionDict = new Dictionary<string, LinkedListNode<MenuOption>>();

		private static Dictionary<string, string> UITextDict = null;

		private static Transform TitleMenu = null;

		private static TitleScreen Title = null;

		internal static void Reset()
		{
			MenuOptions.Clear();
			MenuOptionDict.Clear();
			TitleMenu = null;
			Title = null;
		}

		public static void AddMenuOption(string text, System.Action<TitleScreen> action)
		{
			MenuOptionDict.Add(text, MenuOptions.AddLast(CreateMenuOption(text, action)));
		}

		public static void AddMenuOptionBefore(string text, System.Action<TitleScreen> action, string before)
		{
			LinkedListNode<MenuOption> node = MenuOptionDict[text];
			MenuOptionDict.Add(text, MenuOptions.AddBefore(node, CreateMenuOption(text, action)));
		}

		public static void AddMenuOptionAfter(string text, System.Action<TitleScreen> action, string before)
		{
			LinkedListNode<MenuOption> node = MenuOptionDict[text];
			MenuOptionDict.Add(text, MenuOptions.AddAfter(node, CreateMenuOption(text, action)));
		}

		private static Text[] CreateTextsArray()
		{
			Text[] texts = new Text[MenuOptions.Count];

			int index = 0;

			foreach (MenuOption menuOption in MenuOptions)
				texts[index++] = menuOption.text;

			return texts;
		}

		internal static void SetUITextDict()
		{
			FieldInfo fi = typeof(TextManager).GetField("uiTextDict", Lizard.NonPublicStatic);
			UITextDict = (Dictionary<string, string>)fi.GetValue(null);
		}

		private static string GetUIRefName(string text)
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(text);
			stringBuilder = stringBuilder.Replace(" ", "");
			stringBuilder[0] = char.ToLower(stringBuilder[0]);

			return "titleScreen-" + stringBuilder.ToString();
		}


		private static MenuOption CreateMenuOption(string optionText, System.Action<TitleScreen> _action)
		{
			GameObject gameObject = new GameObject(optionText);

			RectTransform _rectTransform = gameObject.AddComponent<RectTransform>();
			_rectTransform.parent = TitleMenu;
			_rectTransform.anchoredPosition = Vector2.zero;
			
			CanvasRenderer canvasRenderer = gameObject.AddComponent<CanvasRenderer>();

			Text _text = gameObject.AddComponent<Text>();
			_text.alignment = TextAnchor.MiddleCenter;
			_text.color = new Color32(184, 192, 195, 153);
			_text.font = FontData.defaultFontData.font;
			_text.fontSize = 34;
			_text.horizontalOverflow = HorizontalWrapMode.Overflow;
			_text.resizeTextMinSize = 3;
			_text.text = optionText;
			_text.verticalOverflow = VerticalWrapMode.Overflow;
			
			Button _button = gameObject.AddComponent<Button>();
			_button.onClick.AddListener(() => _action(Title));
			_button.transition = Selectable.Transition.None;

			EventTrigger _eventTrigger = gameObject.AddComponent<EventTrigger>();

			Outline outline = gameObject.AddComponent<Outline>();

			string _uiRef = GetUIRefName(optionText);

			if (!UITextDict.ContainsKey(_uiRef))
				UITextDict.Add(_uiRef, optionText);
			
			TextManager.AddUIRef(_uiRef, _text);

			return new MenuOption
			{
				action = _action,
				uiRef = _uiRef,
				rectTransform = _rectTransform,
				text = _text,
				button = _button,
				eventTrigger = _eventTrigger
			};
		}

		private static void AddNullMenuOption(string text, System.Action<TitleScreen> _action)
		{
			string _uiRef = GetUIRefName(text);

			MenuOptionDict.Add(text, MenuOptions.AddLast(new MenuOption { action = _action, uiRef = _uiRef }));
		}

		private static void InitNullMenuOption(string optionText, Text text)
		{
			LinkedListNode<MenuOption> node = MenuOptionDict[optionText];
			MenuOption menuOption = node.Value;
			menuOption.rectTransform = text.rectTransform;
			menuOption.text = text;
			menuOption.button = text.GetComponent<Button>();
			menuOption.eventTrigger = text.GetComponent<EventTrigger>();
			node.Value = menuOption;
		}

		private static int IndexOfMenuOption(string text)
		{
			int index = 0;

			LinkedListNode<MenuOption> node = MenuOptionDict[text];

			for (LinkedListNode<MenuOption> current = MenuOptions.First; current != null; current = current.Next)
			{
				if (current == node)
					return index;

				index++;
			}

			return -1;
		}

		public static void AwakeBegin(TitleScreen titleScreen)
		{
			TitleMenu = titleScreen.transform.Find("TitleMenu");
			Title = titleScreen;

			MenuOptions.Clear();

			AddNullMenuOption("Single Player", OnSinglePlayer);
			AddNullMenuOption("Two Players", OnTwoPlayers);
			AddNullMenuOption("Versus", OnVersus);
			AddNullMenuOption("Options", OnOptions);
			AddNullMenuOption("Credits", OnCredits);

			AddMenuOption("Reload Mods", OnReloadMods);

			AddNullMenuOption("Exit", OnExit);
		}

		public static void AwakeEnd(TitleScreen titleScreen)
		{
			FieldInfo fi = Lizard.GetFieldInfo<TitleScreen>("menuTextOpts", false, false);
			Text[] texts = (Text[])fi.GetValue(titleScreen);

			InitNullMenuOption("Single Player", texts[0]);
			InitNullMenuOption("Two Players", texts[1]);
			InitNullMenuOption("Versus", texts[2]);
			InitNullMenuOption("Options", texts[3]);
			InitNullMenuOption("Credits", texts[4]);
			InitNullMenuOption("Exit", texts[5]);

			fi.SetValue(titleScreen, CreateTextsArray());

			titleScreen.multiplayerOptIndex = IndexOfMenuOption("Two Players");
			titleScreen.versusOptIndex = IndexOfMenuOption("Versus");
			
			SortMenuOptions();
		}

		private static void SortMenuOptions()
		{
			int div = MenuOptions.Count - 1;
			float ySize = 500.0f / div;
			float yFactor = 0.5f / div;

			float yMax = 0.9f;
			int i = 0;

			foreach (MenuOption menuOption in MenuOptions)
			{
				menuOption.rectTransform.offsetMax = Vector2.zero;
				menuOption.rectTransform.offsetMin = Vector2.zero;
				menuOption.rectTransform.sizeDelta = Vector2.zero;
				menuOption.rectTransform.anchorMax = new Vector2(1.0f, yMax);
				menuOption.rectTransform.anchorMin = new Vector2(0.0f, yMax - yFactor);

				menuOption.eventTrigger.triggers.Clear();

				int current = i;

				EventTrigger.Entry entry = new EventTrigger.Entry() { eventID = EventTriggerType.PointerEnter };
				entry.callback.AddListener((x) => Title.SelectMenuIndex(current));

				menuOption.eventTrigger.triggers.Add(entry);

				yMax -= yFactor;
				i++;
			}
		}

		public static bool ConfirmMenuOption(TitleScreen titleScreen)
		{
			if (titleScreen.currentState != TitleScreen.TitleScreenState.Menu)
				return true;

			int index = 0;

			foreach (MenuOption menuOption in MenuOptions)
			{
				if (index++ == titleScreen.currentMenuIndex)
				{
					menuOption.action(titleScreen);
					break;
				}
			}

			return true;
		}

		private static void OnSinglePlayer(TitleScreen titleScreen)
		{
			SoundManager.PlayConfirmAudio();
			titleScreen.StartGame();
		}

		private static void OnTwoPlayers(TitleScreen titleScreen)
		{
			if (!titleScreen.AllowMultiplayer())
			{
				SoundManager.PlayAudio("MenuError");
				GlobalDialog.Display("titleScreen-connect2ndController");
			}
			else
			{
				SoundManager.PlayConfirmAudio();
				titleScreen.StartGame();
				GameController.coopOn = true;
			}
		}

		private static void OnVersus(TitleScreen titleScreen)
		{
			if (!titleScreen.AllowMultiplayer())
			{
				SoundManager.PlayAudio("MenuError");
				GlobalDialog.Display("titleScreen-connect2ndController");
			}
			else
			{
				SoundManager.PlayConfirmAudio();
				titleScreen.StartGame(loadPvP: true);
				GameController.coopOn = true;
			}
		}

		private static void OnOptions(TitleScreen titleScreen)
		{
			titleScreen.currentState = TitleScreen.TitleScreenState.Options;
			GameController.optionsMenu.gameObject.SetActive(value: true);
			GameController.optionsMenu.releaseControlToParent = false;
			titleScreen.Hide();
			SoundManager.PlayConfirmAudio();
		}

		private static void OnCredits(TitleScreen titleScreen)
		{
			Credits.transitionToTitle = true;
			GameController.LoadLevel("Credits");
			SoundManager.PlayConfirmAudio();
		}

		private static void OnReloadMods(TitleScreen titleScreen)
		{
			// unloads mods before loading them
			ModManager.UnloadMods();
			GameController.LoadLevel("TitleScreen");
		}

		private static void OnExit(TitleScreen titleScreen)
		{
			SoundManager.PlayConfirmAudio();
			GameController.Quit();
		}
	}
}
