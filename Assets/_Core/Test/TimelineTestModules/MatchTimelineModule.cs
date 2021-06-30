using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ModuleSystem.Timeline;
using ModuleSystem;
using System;

public class MatchTimelineModule : ModuleTimelineModuleBase
{
	[SerializeField]
	private GameObject[] _targets = null;

	private Dictionary<object, Coroutine> _routines = new Dictionary<object, Coroutine>();

	private int c = 0;

	protected override bool TryProcessTimeline(ModuleAction action, Action unlockMethod)
	{
		if (action is Test.RemoveAction removeAction)
		{
			object obj = new object();
			_routines[obj] = StartCoroutine(RemoveRoutine(removeAction, obj));
			DoCallbackAfterCondition(() => _routines.ContainsKey(obj), unlockMethod);
			return true;
		}

		if (action is Test.HitAction hitAction)
		{
			object obj = new object();
			_routines[obj] = StartCoroutine(HitRoutine(hitAction, obj));
			DoCallbackAfterCondition(() => _routines.ContainsKey(obj), unlockMethod);
			return true;
		}

		if (action is Test.MatchAction matchAction)
		{
			object obj = new object();
			_routines[obj] = StartCoroutine(MatchRoutine(matchAction, obj, c++, unlockMethod));
			return true;
		}
		return false;
	}

	private IEnumerator MatchRoutine(Test.MatchAction matchAction, object obj, int index, Action unlockMethod)
	{
		float t = 0f;
		int targetIndex =  index % _targets.Length;
		GameObject target = _targets[targetIndex];

		if(target == null)
		{
			_routines.Remove(obj);
			unlockMethod();
			yield break;
		}

		bool hitTarget = false;
		while (t < Mathf.PI && target != null)
		{
			target.transform.position = new Vector3(target.transform.position.x, Mathf.Sin(t), 0f);
			t += Time.deltaTime;

			if(t >=(Mathf.PI * 0.5f) && !hitTarget)
			{
				hitTarget = true;
				if(matchAction.TryFindDownwards(null, null, true, out Test.HitAction hitAction))
				{
					hitAction.Index = targetIndex;
					unlockMethod();
				}
			}

			yield return null;
		}
		_routines.Remove(obj);
	}

	private IEnumerator HitRoutine(Test.HitAction hitAction, object obj)
	{
		GameObject target = _targets[hitAction.Index];
		
		if (target == null)
		{
			_routines.Remove(obj);
			yield break;
		}

		if(hitAction.TryFindDownwards(null, null, true, out Test.RemoveAction removeAction))
		{
			removeAction.Index = hitAction.Index;
		}

		SpriteRenderer targetRenderer = target.GetComponent<SpriteRenderer>();
		targetRenderer.color = Color.red;
		yield return new WaitForSeconds(5.5f);
		targetRenderer.color = Color.white;
		_routines.Remove(obj);
	}

	private IEnumerator RemoveRoutine(Test.RemoveAction removeAction, object obj)
	{
		GameObject target = _targets[removeAction.Index];

		if (target == null)
		{
			_routines.Remove(obj);
			yield break;
		}

		SpriteRenderer targetRenderer = target.GetComponent<SpriteRenderer>();
		targetRenderer.color = Color.blue;
		yield return new WaitForSeconds(.5f);
		Destroy(target.gameObject);
		_routines.Remove(obj);
	}
}
