using RaTweening;
using UnityEngine;

public class LambdaExample : MonoBehaviour
{
	protected void Awake()
	{
		// Lambda Float Tween, goes from 5 to 10 in 1 second, logging it every evaluation.
		// Applies a 0.5 seconds delay, Looping infinitely and using an Easing
		RaTweenLambda.TweenFloat(5, 10, 1f, (value, normalizedValue) => Debug.Log(value))
			.SetDelay(0.5f)
			.SetInfiniteLooping()
			.SetEasing(RaEasingType.OutBack);


		// Lambda Rect Tween, tweening a Rect position and size within 2 seconds, logging it every evaluation.
		// Playing it in reverse
		RaTweenLambda.TweenRect(new Rect(0, 0, 10, 50), new Rect(10, 50, 20, 60), 2f, (value, normalizedValue) => Debug.Log(value))
			.SetModifier(RaModifierType.Reverse);
	}
}
