using UnityEngine;
using UnityEngine.UI;

namespace RaTweening.Examples
{
	public class ValuesAndReferencesExample : MonoBehaviour
	{
		[SerializeField]
		private Image _image;

		[SerializeField]
		private Color _startColor;

		[SerializeField]
		private Image _endColorRef;

		private void Awake()
		{
			// Starts a Color Tween, but we set the end value as default
			// Then we set the End Reference to `_endColorRef`. 
			// When the Tween starts, it will tween to the reference's color!
			_image.TweenColor(_startColor, default, 2f)
				.SetEndRef(_endColorRef);
		}
	}

}