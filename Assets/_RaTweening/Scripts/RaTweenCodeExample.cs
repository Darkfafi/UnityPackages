using RaTweening;
using UnityEngine;
using UnityEngine.UI;

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


	public class EasingsExample : MonoBehaviour
	{
		[SerializeField]
		private Transform _target;

		[SerializeField]
		private AnimationCurve _easingCurve;

		private void Awake()
		{
			// Starts a Move Tween, from current position to [1, 1, 1]
			// Then sets the Easing Curve to `OutBack`
			_target.TweenMove(Vector3.one, 2f).SetEasing(RaEasingType.OutBack);
			// Or use a Unity AnimationCurve
			_target.TweenMove(Vector3.one, 2f).SetEasing(_easingCurve);
		}
	}


	public class ModifierExample : MonoBehaviour
	{
		[SerializeField]
		private Transform _target;

		[SerializeField]
		private AnimationCurve _modifierCurve;

		private void Awake()
		{
			// Starts a Move Tween, from current position to [1, 1, 1]
			// Then sets the modifier to play the tween in Reverse
			_target.TweenMove(Vector3.one, 2f).SetModifier(RaModifierType.Reverse);
			// Or use a Unity AnimationCurve
			_target.TweenMove(Vector3.one, 2f).SetModifier(_modifierCurve);
		}
	}

	public class SequenceExample : MonoBehaviour
	{
		[SerializeField]
		private Transform _target;

		private void Awake()
		{
			// Create a Sequence
			RaTweenSequence sequence = RaTweenSequence.Create();

			// Appends a Y Axis Move Tween to the Sequence
			// And on 50% of the Tween, start the next entry in the Sequence
			sequence.Append(_target.TweenMoveY(2, 1f).ToSequenceEntry(0.5f));

			// Appends a Z Axis Rotation Tween to the Sequence 
			// Starts halfway the movement due to the stagger by the previous tween
			sequence.Append(_target.TweenRotateZ(30, 1f).ToSequenceEntry());

			// Appends a Callback to the Sequence
			sequence.AppendCallback(() => { Debug.Log("Log Callback!"); });

		}
	}

	public class EventsExample : MonoBehaviour
	{
		[SerializeField]
		private Transform _target;

		private void Awake()
		{
			// Create a Movement Tween after 5 seconds
			RaTween moveTween = _target.TweenMoveY(2, 1f).SetDelay(5f);

			// Callback for when the tween is Registered (at the start of the delay)
			moveTween.OnSetup(() => Debug.Log("On Setup"));

			// Callback for when the tween has Started (after the delay)
			moveTween.OnStart(() => Debug.Log("On Start"));

			// Callback for when the tween is Evaluated (a step is applied)
			moveTween.OnUpdate(() => Debug.Log("On Update"));

			// Callback for when the tween is Looped (for finite or infinite looping tweens)
			moveTween.OnLoop((loopCount) => Debug.Log($"On Loop {loopCount}"));

			// Callback for when the tween is Completed (end of the tween)
			moveTween.OnComplete(() => Debug.Log("On Complete"));

			// Callback for when the tween is Killed (Cancelled or after Completion)
			moveTween.OnKill(() => Debug.Log("On Kill"));
		}
	}

	public class SequenceEventsExample : MonoBehaviour
	{
		[SerializeField]
		private Transform _target;

		private void Awake()
		{
			// Creates a Sequence with 3 tweens
			RaTweenSequence.Create
			(
				_target.TweenMoveY(2, 1f).SetDelay(2f).ToSequenceEntry(),
				_target.TweenMoveX(2, 1f).ToSequenceEntry(),
				_target.TweenMoveY(0, 1f).ToSequenceEntry()
			)
			// Callback for when the Sequence has Started Processing the First Tween
			.OnStart(() => { Debug.Log("Sequence Started"); })
			// Callback for when the Sequence has Completed Processing the Last Tween
			.OnComplete(() => Debug.Log("Sequence Completed"));
		}
	}

}