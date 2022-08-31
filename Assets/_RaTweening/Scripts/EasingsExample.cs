using UnityEngine;

namespace RaTweening.Examples
{
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

}