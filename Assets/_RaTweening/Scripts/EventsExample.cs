using UnityEngine;

namespace RaTweening.Examples
{
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

}