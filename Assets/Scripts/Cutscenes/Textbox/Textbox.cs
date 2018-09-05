using CharTween;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutscenes.Textboxes {
	public class Textbox : MonoBehaviour {
		private const char ZERO_WIDTH_SPACE = '​'; // lol
		private const int OPEN_TAG_LENGTH = 3;
		private const int CLOSE_TAG_LENGTH = 4;

		[SerializeField]
		private TMP_Text text;

		[SerializeField]
		private Image box;

		[SerializeField]
		private TextMeshProUGUI leftName;

		[SerializeField]
		private TextMeshProUGUI rightName;

		private List<Tween> currentEffects = new List<Tween>();

		private IDictionary<TextEffect, MatchCollection> effectSubstrings 
			= new Dictionary<TextEffect, MatchCollection>();

		public void AddText(CutsceneSide nameSide, string speaker, string message) {
			ResetDictionary();
			foreach (Tween tween in currentEffects) {
				tween.Kill(true);
			}
			currentEffects.Clear();

			if (nameSide == CutsceneSide.FarLeft || nameSide == CutsceneSide.Left) {
				leftName.SetText(speaker);
				rightName.SetText(string.Empty);
			} else if (nameSide == CutsceneSide.FarRight || nameSide == CutsceneSide.Right) {
				rightName.SetText(speaker);
				leftName.SetText(string.Empty);
			}

			char[] chars = message.ToCharArray();

			// parse out tagged strings
			foreach (TextEffect te in TextEffect.All) {
				MatchCollection matches = Util.GetTaggedSubstrings(te.symbol, message);

				// Replace tags with zero width space to maintain index accuracy bc lazy
				foreach (Match match in matches) {
					for (int i = match.Index - OPEN_TAG_LENGTH; i < match.Index; i++) {
						chars[i] = ZERO_WIDTH_SPACE;
					}
					for (int i = match.Index + match.Length; i < match.Index + match.Length + CLOSE_TAG_LENGTH; i++) {
						chars[i] = ZERO_WIDTH_SPACE;
					}
				}
				effectSubstrings[te] = matches;
			}

			text.SetText(new string(chars));

			StartCoroutine(AnimateText());
		}

		// CharTweener black magic happens here.
		private IEnumerator AnimateText() {
			CharTweener charTweener = text.GetCharTweener();

			Tween[] resets = GetResetTweens(charTweener);
			foreach (KeyValuePair<TextEffect, MatchCollection> pair in effectSubstrings) {
				foreach (Match match in pair.Value) {
					for (int i = match.Index; i < (match.Index + match.Length); i++) {
						resets[i].Kill();

						var timeOffset = Mathf.Lerp(0, 1, i / (float)(text.text.Length - 1));

						Tween tween = pair.Key.DoEffect(i, charTweener);
						currentEffects.Add(tween);

						tween.fullPosition += timeOffset;
					}
				}
			}

			for (int i = 0; i < text.text.Length; i++) {

				// Otherwise we'll linger too long on zero width spaces
				while (i < text.text.Length && text.text[i] == ZERO_WIDTH_SPACE) {
					i++;
				}
				text.maxVisibleCharacters = (i + 1);
				yield return null;
			}
		}

		public void AddText(string message) {
			AddText(CutsceneSide.None, string.Empty, message);
		}

		// Reset state between runs so we don't have random colors from last call everywhere
		private Tween[] GetResetTweens(CharTweener charTweener) {
			Tween[] resets = new Tween[text.text.Length];
			for (int i = 0; i < text.text.Length; i++) {
				resets[i] =
					DOTween.Sequence()
					.Append(charTweener.DOColor(i, Color.white, 0))
					.Append(charTweener.DOCircle(i, 0, 0));
			}
			return resets;
		}

		private void ResetDictionary() {
			effectSubstrings.Clear();
			foreach (TextEffect te in TextEffect.All) {
				effectSubstrings.Add(te, null);
			}
		}
	}
}
