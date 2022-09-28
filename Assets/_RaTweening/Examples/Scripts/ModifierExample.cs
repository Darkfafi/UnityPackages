using UnityEngine;

namespace RaTweening.Examples
{
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

}