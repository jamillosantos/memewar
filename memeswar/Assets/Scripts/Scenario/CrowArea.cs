﻿using UnityEngine;
using System.Collections;
using Memewars;

/// <summary>
/// Define a área onde o corvo será acionado.
/// </summary>
public class CrowArea : MonoBehaviour
{
	void OnTriggerEnter(Collider other)
	{
		StickmanCharacter player = other.gameObject.GetComponent<StickmanCharacter>();
		if (player != null)
		{
			Crow[] crows = this.transform.root.gameObject.GetComponentsInChildren<Crow>();
			foreach (Crow c in crows)
				c.Play();
		}
	}
}
