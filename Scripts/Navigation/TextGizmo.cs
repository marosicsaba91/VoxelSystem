using UnityEngine;

public static class TextGizmos
{
	static readonly GUIStyle style = new()
	{
		fontSize = 12,
		normal = new GUIStyleState
		{
			textColor = Color.white
		}
	};

	public static int FontSize
	{
		get => style.fontSize;
		set => style.fontSize = value;
	}

	public static Color TextColor
	{
		get => style.normal.textColor;
		set => style.normal.textColor = value;
	}


	public static void DrawText(Vector3 position, string text)
	{
#if UNITY_EDITOR
		UnityEditor.Handles.Label(position, text, style); 
#endif
	}
}