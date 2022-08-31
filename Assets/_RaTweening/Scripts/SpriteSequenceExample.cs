using UnityEngine;

namespace RaTweening.Examples
{
	public class SpriteSequenceExample : MonoBehaviour
	{
		[SerializeField]
		private SpriteRenderer _spriteRenderer;

		[SerializeField]
		private Sprite[] _sprites;

		private void Awake()
		{
			// Starts a Sprite Sequence Animation in 1 second
			// It will take 3 seconds to animate through all the sprites
			// After the animation it will log 'Completed!'
			_spriteRenderer.TweenSpriteSequence(_sprites, 3f)
				.SetDelay(1f)
				.OnComplete(() =>
				{
					Debug.Log("Completed!");
				});
		}
	}
}