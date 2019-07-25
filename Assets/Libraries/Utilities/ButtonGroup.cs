using Sojourn.PicnicIOC;
using Sojourn.Extensions;
using UnityEngine;
using UnityEngine.UI;
using AOFL.Promises.V1.Core;
using AOFL.Promises.V1.Interfaces;
using System.Collections;
using System.Collections.Generic;
using TMPro;

//`Mat add option for a limit to the lowest min and highest max
namespace Sojourn.Utility {
	[RequireComponent(typeof(CanvasGroup))]
	[RequireComponent(typeof(Button))]
	public class ButtonGroup : MonoBehaviour {
		[SerializeField]
		private TMP_Text _text = null;

		private Button _button = null;
		private CanvasGroup _canvasGroup;

		public Button Button { get => _button; }
		public CanvasGroup Group { get => _canvasGroup; }
		public TMP_Text Text { get => _text; }

		private void Awake() {
			_canvasGroup = GetComponent<CanvasGroup>();
			_button = GetComponent<Button>();
		}

	}
}
