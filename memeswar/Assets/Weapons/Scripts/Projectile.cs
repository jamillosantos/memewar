﻿using Memewars;
using System;
using UnityEngine;

public class CollisionInfo
{
	public StickmanCharacter StickmanCharacter;

	public Weapon Weapon;

	public Vector3 Point;

	public Vector3 Normal;

	public CollisionInfo(ContactPoint contactPoint)
	{
		this.Point = contactPoint.point;
		this.Normal = contactPoint.normal;
	}

	public CollisionInfo(StickmanCharacter stickmanCharacter, Weapon weapon,  Vector3 point, Vector3 normal)
	{
		this.StickmanCharacter = stickmanCharacter;
		this.Weapon = weapon;
		this.Point = point;
		this.Normal = normal;
	}
}

/// <summary>
/// Classe que implementa o comportamento básico de um projétil (movimentação inicial, colisões
/// e danos).
/// 
/// OBS: A movimentação do projétil será definida e controlada pela classe `ProjectileTrajectory`
/// e suas especializações.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(PhotonView))]
public class Projectile : Photon.MonoBehaviour
{
	/// <summary>
	/// Dano infligido caso acerte um jogador.
	/// </summary>
	public float Damage;

	/// <summary>
	/// Velocidade do disparo.
	/// </summary>
	public float Speed;

	/// <see cref="Collided" />
	private bool _collided = false;

	/// <summary>
	/// Flag que guarda se o objeto já sofreu alguma colisão.
	/// </summary>
	protected bool Collided
	{
		get
		{
			return this._collided;
		}
	}

	/// <see cref="Fired" />
	private bool _fired = false;

	/// <see cref="FiredAt" />
	private float _firedAt = -1;

	private Vector3 _velocity;

	/// <see cref="DefaultCollider" />
	private Collider _defaultCollider;

	private StickmanCharacter _stickmanCharacter;

	private Weapon _weapon;

	/// <summary>
	/// Colisor padrão do projétil.
	/// </summary>
	public Collider DefaultCollider
	{
		get
		{
			return this._defaultCollider;
		}
	}

	public StickmanCharacter StickmanCharacter
	{
		get
		{
			return this._stickmanCharacter;
		}
	}

	public Weapon Weapon
	{
		get
		{
			return this._weapon;
		}
	}

	/// <see cref="DefaultRenderer"/>
	private Renderer _defaultRenderer;

	/// <summary>
	/// Referência padrão para o renderer do projétil.
	/// </summary>
	public Renderer DefaultRenderer
	{
		get
		{
			return this._defaultRenderer;
		}
	}


	/// <summary>
	/// Velocidade vetorial do objeto.
	/// </summary>
	public Vector3 Velocity
	{
		get
		{
			return this._velocity;
		}
	}

	/// <summary>
	/// Se o objeto foi disparado ou não.
	/// </summary>
	public bool Fired
	{
		get
		{
			return this._fired;
		}
	}

	/// <summary>
	/// Momento em que o projétil foi lançado.
	/// </summary>
	public float FiredAt
	{
		get
		{
			return this._firedAt;
		}
	}

	/// <summary>
	/// Resource que vai ser instanciado quando houver o hit.
	/// </summary>
	public GameObject CollisionFX;

	protected virtual void Start()
	{
		this._defaultRenderer = this.GetComponent<Renderer>();
		Collider[] colliders = this.GetComponents<Collider>();
		foreach (Collider c in colliders)
			c.enabled = this.photonView.isMine;
		this._defaultCollider = colliders[0];
	}

	protected virtual void Update()
	{ }

	public void Fire(StickmanCharacter stickmanCharacter, Weapon weapon, Vector3 direction)
	{
		this._stickmanCharacter = stickmanCharacter;
		this._weapon = weapon;
		this._firedAt = Time.timeSinceLevelLoad;
		this._velocity = direction * this.Speed;
		this._fired = true;
	}

	/// <summary>
	/// Método chamado pelo Unity para tratar a colisão.
	/// </summary>
	/// <param name="collision">Objeto que contém todos os dados da colisão.</param>
	/// <see cref="Hit" />
	public virtual void OnCollisionEnter(Collision collision)
	{
		Debug.Log(Time.timeSinceLevelLoad + " - Collision: " + collision.gameObject.name);
		this._collided = true;
		this.Hit(collision);
	}

	public virtual void OnTriggerEnter(Collider other)
	{
		Debug.Log(Time.timeSinceLevelLoad + " - Collision: " + other.gameObject.name);
	}

	/// <summary>
	/// Método interno utilizado para tratar a colisão.
	/// </summary>
	/// <param name="collision">Objeto que contém todos os dados da colisão.</param>
	protected virtual void Hit(Collision collision)
	{
		if (this.photonView.isMine)
		{
			Damageable damageable;
			foreach (ContactPoint contact in collision.contacts)
			{
				damageable = contact.otherCollider.GetComponent<Damageable>();
				if (damageable)
					 this.ApplyDamage(contact, damageable);
			}
			this.DestroyVisualEffect(collision.contacts[0].point);
			this.photonView.RPC("DestroyProjectile", PhotonTargets.Others, collision.contacts[0].point);
			Destroy(this.gameObject);
		}
	}

	private void DestroyVisualEffect(Vector3 point)
	{
		Instantiate(this.CollisionFX, point, Quaternion.identity);
	}

	/// <summary>
	/// Aplica o dano ao objeto.
	/// </summary>
	/// <param name="contact">Ponto de contato com o `collider`.</param>
	/// <param name="damageable">Objeto que sofrerá o dano.</param>
	protected virtual void ApplyDamage(ContactPoint contact, Damageable damageable)
	{
		Debug.Log("Recreasing " + this.Damage + " from " + damageable.CurrentHP);
		Vector3 normal = (this.transform.position - contact.point);
		normal.Normalize();
		damageable.Damage(this.Damage, new CollisionInfo(this.StickmanCharacter, this.Weapon, contact.point, normal));
	}

	protected virtual void OnDestroy()
	{ }

	[PunRPC]
	protected virtual void DestroyProjectile(Vector3 point)
	{
		this.DestroyVisualEffect(point);
		Destroy(this.gameObject);
	}
}

/// <summary>
/// 
/// </summary>
public class ProjectileTrajectory : MonoBehaviour
{
	protected Rigidbody _rigidbody;

	protected Projectile _projectile;

	protected virtual void Start()
	{
		this._projectile = this.GetComponent<Projectile>();
		this._rigidbody = this.GetComponent<Rigidbody>();
	}
}

