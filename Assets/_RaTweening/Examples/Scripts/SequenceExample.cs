using UnityEngine;

namespace RaTweening.Examples
{
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

}