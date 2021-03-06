﻿using Memewars;
using System;
using UnityEngine;


/// <summary>
/// Especialização da classe `Weapon` para armas de fogo.
/// </summary>
/// <see cref="Weapon"/>
public class Gun
	: Weapon
{
	/// <summary>
	/// Carga atual de munição na arma, em unidades.
	/// </summary>
	public float Ammo;

	/// <summary>
	/// Tamanho do cartucho da arma, em unidades. Ex: Shotgun tem 6 tiros.
	/// </summary>
	public float CartridgeSize;

	/// <summary>
	/// O tempo que demora para recarregar a arma.
	/// </summary>
	public float ReloadTime;

	/// <summary>
	/// Quantidade de munição que será carregada a cada processo de carregamento. Ex: Shotgun vai recarregar
	/// 1 unidade para cada tempo de carregamento, equanto a AK-47 irá carregar as 30 unidades de seu pente
	/// por vez.
	/// </summary>
	public float ReloadAmount;

	/// <summary>
	/// Força de recuo da arma quando atirando no solo.
	/// </summary>
	public float GroundedRecoilForce = 1f;

	/// <summary>
	/// Força de recuo da arma quando atirado no ar.
	/// </summary>
	public float AirRecoilForce = 1f;

	/// <see cref="LastShotAt" />
	public float _lastShotAt;

	/// <summary>
	/// Momento em que o último tiro foi disparado.
	/// </summary>
	public float LastShotAt
	{
		get
		{
			return this._lastShotAt;
		}
	}

	public override bool IsFull
	{
		get
		{
			return (this.Ammo == this.CartridgeSize);
		}
	}

	/// <summary>
	/// Quantos segundos se passaram a partir do último tiro dado. Esta variável será atualizada quando o processo
	/// de reload for finalizado.
	/// </summary>
	public float LastShotElapsed
	{
		get
		{
			return (Time.time - this._lastShotAt);
		}
	}

	/// <summary>
	/// Prefab do projétil que será disparado.
	/// </summary>
	public GameObject BulletPrefab;

	/// <summary>
	/// Spawn point do projétil.
	/// </summary>
	public GameObject BulletSpawnPoint;

	/// <summary>
	/// Sistema de partículas que será acionado no momento do tiro.
	/// </summary>
	public ParticleSystem MuzzleParticleSystem;

	/// <see cref="Weapon.CreateTrigger1"/>
	protected override Trigger CreateTrigger1()
	{
		return new GunTrigger();
	}

	/// <see cref="Weapon.CreateTrigger2"/>
	protected override Trigger CreateTrigger2()
	{
		return new GunTrigger();
	}

	protected GunTrigger GunTrigger1
	{
		get
		{
			return (GunTrigger)this.Trigger1;
		}
	}

	protected GunTrigger GunTrigger2
	{
		get
		{
			return (GunTrigger)this.Trigger2;
		}
	}

	/// <summary>
	/// Decrementa a arma para o Trigger1.
	/// </summary>
	protected virtual void DecreaseAmmo1()
	{
		this.Ammo -= 1f;
	}

	/// <summary>
	/// Decrementa a arma para o Trigger2.
	/// </summary>
	protected virtual void DecreaseAmmo2()
	{
		this.Ammo -= 1f;
	}

	/// <summary>
	/// Cria os projéteis que serão disparados para o Trigger1.
	/// </summary>
	public virtual void CreateProjectile1(Vector3[] directions, Vector3[] positions)
	{
		/// Aplica a força do refugo da arma (no chão e no ar)
		if (this.StickmanCharacter.IsGrounded)
			this.StickmanCharacter.rootRigidbody.velocity += (this.StickmanCharacter.AimDirection * -1 * this.GroundedRecoilForce);
		else
			this.StickmanCharacter.rootRigidbody.velocity += (this.StickmanCharacter.AimDirection * -1 * this.AirRecoilForce);

		this.VisualFireEffect();
		/// Cria os diversos tiros nas direções informadas.
		/// Os tiros serão criados via rede.
		/// Normalmente o array deverá ter apenas um elemento. Salve armas estilo a shotgun.
		for (uint i = 0; i < directions.Length; i++)
		{
			GameObject bullet = PhotonNetwork.Instantiate(this.BulletPrefab.name, positions[i], Quaternion.identity, 0, new object[] {
				directions[i]
			});
		}
	}

	/// <summary>
	/// Cria os projéteis que serão disparados para o Trigger2.
	/// </summary>
	protected virtual void CreateProjectile2(Vector3 direction, Vector3 position)
	{
		/// Implementação ficou de fora.
	}

	/// <summary>
	/// Método que ativa a ação de atirar para o Trigger1.
	/// </summary>
	protected virtual void Fire1()
	{
		this._lastShotAt = Time.time;
		this.DecreaseAmmo1();
		this.TriggerCreateProjectile1();
	}

	protected virtual void TriggerCreateProjectile1()
	{
		// int[] networkIds = new int[] { PhotonNetwork.AllocateViewID() };
		Vector3[] directions = new Vector3[] { this.StickmanCharacter.AimDirection };
		Vector3[] positions = new Vector3[] { this.BulletSpawnPoint.transform.position };
		this.CreateProjectile1(directions, positions);
		// this.StickmanCharacter.photonView.RPC("CreateProjectile1", PhotonTargets.Others, networkIds, directions, positions);
	}

	/// <summary>
	/// Método que ativa a ação de atirar para o Trigger2.
	/// </summary>
	protected virtual void Fire2()
	{
		Debug.Log("Fire2");
		this._lastShotAt = Time.time;
		this.DecreaseAmmo2();
		this.CreateProjectile2(this.StickmanCharacter.AimDirection, this.BulletSpawnPoint.transform.position);
	}

	/// <summary>
	/// Verifica os triggers e dispara os métodos `Fire` correspondente dependendo das condições de tiro da arma.
	/// </summary>
	public virtual void Update()
	{
		if (this.IsReloading)
		{
			/// Se o processo de reloading foi concluido.
			if (this.ReloadingElapsed >= this.ReloadTime)
			{
				this.Ammo = Mathf.Min(this.Ammo + this.ReloadAmount, this.CartridgeSize);
				this.StopReloading();
			}
		}
		else
		{
			// Se puder atirar com gatilho 1
			if (this.CanFire((GunTrigger)this.Trigger1))
			{
				this.Fire1();
			}
			// Se puder atirar com gatilho 2
			else if (this.CanFire((GunTrigger)this.Trigger2))
			{
				this.Fire2();
			}
		}
	}

	/// <summary>
	/// Verifica se é possível atirar, dadas as condições atuais da arma.
	/// </summary>
	/// <param name="trigger">Gatilho que será utilizado para a verificação.</param>
	/// <returns>Se é possível ou não atirar.</returns>
	protected bool CanFire(GunTrigger trigger)
	{
		return
			trigger.Pulled
			&& (!this.IsReloading)                                      // Não carregando
			&& (this.Ammo > 0)                                          // Munição disponível
			&& (trigger.PulledElapsed >= trigger.TimePrepareFirstShot)	// Tempo de preparação já ocorrido
			&& (this.LastShotElapsed >= trigger.TimeBetweenShots);      // Entre o último tiro e este já passou tempo suficiente
	}

	/// <summary>
	/// Dispara o Muzzle effect da arma quando um tiro for acionado.
	/// </summary>
	public override void VisualFireEffect()
	{
		this.MuzzleParticleSystem.Play();
	}
}
