﻿using UnityEngine;
using System.Collections.Generic;

namespace Memewars
{
	/// <summary>
	/// Classe ajudante para manipulação dos PohtonPlayers.
	/// </summary>
	public class Players
	{
		static Dictionary<int, StickmanCharacter> players = new Dictionary<int, StickmanCharacter>();

		public static void Set(PhotonPlayer player, StickmanCharacter stickman)
		{
			players[player.ID] = stickman;
		}

		public static StickmanCharacter Get(PhotonPlayer player)
		{
			return players[player.ID];
		}
	}


	/// <summary>
	/// Abstração do personagem.
	/// </summary>
	[RequireComponent(typeof(Animator))]
	public class StickmanCharacter : Photon.MonoBehaviour
	{
		private Transform _hudTransform;

		private CanvasKill[] _canvasKill;

		static UnityEngine.Object BloodFountain;

		/// <summary>
		/// Array que guarda as referências das armas do arsenal.
		/// </summary>
		protected Weapon[] Arsenal = new Weapon[5];

		/// <summary>
		/// Mantém os tipos das armas que serão utilizados no arsenal.
		/// </summary>
		private Weapon.Weapons[] ArsenalType = new Weapon.Weapons[5] {
			Weapon.Weapons.AK47, Weapon.Weapons.AK47, Weapon.Weapons.AK47, Weapon.Weapons.AK47, Weapon.Weapons.AK47
		};

		/// <summary>
		/// </summary>
		/// <see cref="Weapon"/>
		private Weapon _currentWeapon;

		/// <summary>
		/// </summary>
		/// <see cref="WeaponIndex"/>
		private int _weaponIndex = -1;

		/// <summary>
		/// Índice da arma utilizada anteriormente. Utilizada no método ToggleWeapon.
		/// </summary>
		private int _lastWeaponIndex = -1;

		private int _deaths = 0;

		/// <summary>
		/// Quantidade de mortes de cada jogador (sofridas).
		/// </summary>
		public int Deaths
		{
			get
			{
				object deaths;
				if (this.photonView.owner.customProperties.TryGetValue("Deaths", out deaths))
					return (int)deaths;
				else
					return 0;
			}
		}

		private int _kills = 0;

		/// <summary>
		/// Quantidade de assasinatos de cada jogador (cometidos).
		/// </summary>
		public int Kills
		{
			get
			{
				return this.photonView.owner.GetScore();
			}
		}

		private float _lastKillAt;

		/// <summary>
		/// Momento da última morte do jogador para utilização no sistema de "façanha".
		/// </summary>
		public float LastKillAt
		{
			get
			{
				return this._lastKillAt;
			}
		}

		/// <summary>
		/// Tempo decorrido da última morte.
		/// </summary>
		public float TimeSinceLastKill
		{
			get
			{
				return (Time.timeSinceLevelLoad - this._lastKillAt);
			}
		}

		public float MaxHorizontalSpeed = 7f;

		/// <summary>
		/// Força do pulo.
		/// ( padrão do script do unity)
		/// </summary>
		[SerializeField]
		float _jumpPower = 9f;

		/// ( padrão do script do unity)
		[Range(1f, 4f)]
		[SerializeField]
		float _runCycleLegOffset = 0.2f; //specific to the character in sample assets, will need to be modified to work with others

		/// ( padrão do script do unity)
		[SerializeField]
		float _groundCheckDistance = 0.1f;

		/// <summary>
		/// Função que replica a ragdoll na posição do jogador.
		/// </summary>
		public void Ragdoll()
		{
			UnityEngine.Object obj = Resources.Load("Ragdoll");
			RagdollController ragdoll = ((GameObject)Instantiate(obj, this._rootRigidbody.transform.position, this._rootRigidbody.transform.rotation)).GetComponent<RagdollController>();
			ragdoll.transform.SetParent(null, true);
			ragdoll.Mimic(this);
			this.gameObject.SetActive(false);
		}

		private Rigidbody _rootRigidbody;
		private Collider _rootCollider;

		public Collider RootCollider
		{
			get
			{
				return this._rootCollider;
			}
		}

		private Animator _animator;

		private bool _isGrounded = true;
		
		private float _origGroundCheckDistance;
		
		private const float _half = 0.5f;

		/*
		float m_CapsuleHeight;
		Vector3 m_CapsuleCenter;
		CapsuleCollider m_Capsule;
		*/

		private float _jumpTime = 0;
		private float _jetpackTime = 0;

		/// <summary>
		/// Capacidade máxima do jetpack.
		/// </summary>
		private float _jetpackCapacity = 30f;

		/// <summary>
		/// Quanto tempo demora o carregamento do jetpack.
		/// </summary>
		private float _jetpackReloadDuration = 5f;

		/// <summary>
		/// Reproduz um som. Usado para o som das armas.
		/// </summary>
		/// <param name="audioSdx">Clip que será reproduzido.</param>
		public void PlayOneShot(AudioClip audioSdx)
		{
			this._audioSource.PlayOneShot(audioSdx);
		}

		/// <summary>
		/// Valor atual do combustível do jetpack.
		/// </summary>
		private float _jetpackFuel;

		/// <summary>
		/// Ratio de recarregamento. Variável constante baseada na capacidade total do tanque pelo tempo de carregamento.
		/// </summary>
		private float _jetpackReloadRatio;

		private bool _started = false;

		/// <summary>
		/// Posição cache vinda da network.
		/// <see cref="OnPhotonSerializeView(PhotonStream, PhotonMessageInfo)"/>
		/// </summary>
		private Vector3 _updatedPosition;

		/// <summary>
		/// Rotação cache vinda da network.
		/// <see cref="OnPhotonSerializeView(PhotonStream, PhotonMessageInfo)"/>
		/// </summary>
		private Quaternion _updatedRotation;

		/// <summary>
		/// Velocidade cache vinda da network.
		/// <see cref="OnPhotonSerializeView(PhotonStream, PhotonMessageInfo)"/>
		/// </summary>
		private Vector3 _updatedVelocity;

		/// <summary>
		/// Diração da mira cache vinda da network.
		/// <see cref="OnPhotonSerializeView(PhotonStream, PhotonMessageInfo)"/>
		/// </summary>
		private Vector3 _updatedAimDirection;

		private Bar _jetpackUIBar;
		private Bar _ammoUIBar;

		private Vector3 AimOffset = Vector3.up * 1.3f;

		private Arsenal _arsenalPlaceholder;

		/// <summary>
		/// Referência do `ParticleSystem` das chamas do jetpack.
		/// </summary>
		private ParticleSystem _jetpackFlames;

		/// <summary>
		/// Referência do `ParticleSystem` da fumaça do jetpack.
		/// </summary>
		private ParticleSystem _jetpackSmoke;

		/// <summary>
		/// Referência da luz do jetpack.
		/// </summary>
		private Light _jetpackLight;

		private HeadSprite _head;

		/// <summary>
		/// Retorna se o jogador está no chão ou não.
		/// </summary>
		public bool IsGrounded
		{
			get
			{
				return this._isGrounded;
			}
		}

		[PunRPC]
		public void SetArsenal(Weapon.Weapons[] arsenal)
		{
			if (!this.photonView.isMine)
			{
				foreach (Weapon.Weapons w in arsenal)
					Debug.Log(Time.timeSinceLevelLoad + ": SetArsenal " + w);
			}
			this.ArsenalType = arsenal;

			int i = 0;
			foreach (Weapon.Weapons w in arsenal)
			{
				this.ReplaceWeapon(i, w);
				i++;
			}
			if (this.photonView.isMine && this._started)
				this.photonView.RPC("SetArsenal", PhotonTargets.Others, arsenal);
		}

		void OnPhotonInstantiate(PhotonMessageInfo info)
		{
			Debug.Log("Instantiate " + this.photonView.isMine);
			if (this.photonView.isMine)
			{
				Debug.Log("Enviando dados!");
				this.photonView.RPC("SetArsenal", PhotonTargets.Others, this.ArsenalType);
			}
		}

		[PunRPC]
		public void ReplaceWeapon(int index, Weapon.Weapons weapon)
		{
			if (this._arsenalPlaceholder)
			{
				if (this.Arsenal[index])
					PhotonNetwork.Destroy(this.Arsenal[index].gameObject);
				UnityEngine.Object original = Resources.Load(weapon.ToString());
				GameObject go = (GameObject)Instantiate(original, Vector3.zero, Quaternion.Euler(0, -90, 0));
				this.Arsenal[index] = go.GetComponent<Weapon>();
				go.transform.SetParent(this._arsenalPlaceholder.gameObject.transform, false);
				go.SetActive(false);
			}
			else
				this.ArsenalType[index] = weapon;
			
			if (this.photonView.isMine)
				this.photonView.RPC("ReplaceWeapon", PhotonTargets.Others, index, weapon);
		}

		/// <see cref="JetpackOn" />
		private bool _jetpackOn = false;

		/// <see cref="AimDirection" />
		private Vector3 _aimDirection = Vector3.zero;

		private float _aimAngle;

		/// <summary>
		/// Altura da cabeça do boneco.
		/// </summary>
		private readonly float HEAD_HEIGHT = 1.3f;

		private CameraFollower _cameraFollower;

		public Rigidbody rootRigidbody
		{
			get
			{
				return this._rootRigidbody;
			}
		}

		/// <summary>
		/// Variável que define se o Jetpack está ligado ou não.
		/// </summary>
		public bool JetpackOn
		{
			get
			{
				return this._jetpackOn;
			}
			set
			{
				if (this._jetpackOn != value)
				{
					this._jetpackOn = value;
					//this._jetpackLight.gameObject.SetActive(value);
					if (value)
						this._jetpackFlames.Play(true);
					else
						this._jetpackFlames.Stop(true);
				}
			}
		}

		/// <summary>
		/// Variável que garda a arma atual selecionada. Apenas para facilitar o acesso.
		/// </summary>
		public Weapon Weapon
		{
			get
			{
				return this._currentWeapon;
			}
		}

		/// <summary>
		/// Variável que guarda o índice da arma atual.
		/// </summary>
		public int WeaponIndex
		{
			get
			{
				return this._weaponIndex;
			}

			set
			{
				if (value != this._weaponIndex)
				{
					if ((value >= 0) && (value < this.Arsenal.Length))
					{
						if (this.Arsenal[value] == null)
							this.ReplaceWeapon(value, this.ArsenalType[value]);

						this._lastWeaponIndex = this._weaponIndex;
						this._weaponIndex = value;
						this.UpdateWeapon(this.Arsenal[value]);
						if ((this._ammoUIBar) && (this.Weapon) && (this.Weapon is Gun))
							this._ammoUIBar.Max = ((Gun)this.Weapon).CartridgeSize;
					}
					else
						throw new System.IndexOutOfRangeException("O índice da arma está fora do arsenal.");
				}
				if (this.photonView.isMine)
					this.photonView.RPC("SetWeaponIndex", PhotonTargets.Others, value);
			}
		}

		[PunRPC]
		public void SetWeaponIndex(int value)
		{
			this.WeaponIndex = value;
		}

		/// <summary>
		/// Troca de armas entre as duas recentemente atualizadas.
		/// </summary>
		public void ToggleWeapon()
		{
			if (this._lastWeaponIndex >= 0)
				this.WeaponIndex = this._lastWeaponIndex;
		}

		public void VisualFireEffect()
		{
			if (this.Weapon)
			{
				this.Weapon.VisualFireEffect();
			}
		}

		void Update()
		{
			if (this.photonView.isMine)
			{
				if (this.Weapon)
				{
					if (this.Weapon.IsReloading)
					{
						if (this.Weapon is Gun)
						{
							Gun g = (Gun)this.Weapon;
							this._ammoUIBar.Current = Mathf.Min(this._ammoUIBar.Max, g.Ammo + (g.ReloadAmount * (g.ReloadingElapsed / g.ReloadTime)));
						}
					}
					else if (this.Weapon is Gun)
						this._ammoUIBar.Current = ((Gun)this.Weapon).Ammo;
					
					if (this.Weapon.Trigger1.Pulled)
					{
						this._musicController.StartCombat();
					}
				}
			}
		}

		/// <summary>
		/// Método chamado para atualização da arma. Ele não é chamado diretamente mas sim pelo setter de `WeaponIndex`.
		/// </summary>
		/// <see cref="WeaponIndex"/>
		/// <param name="newWeapon"></param>
		protected void UpdateWeapon(Weapon newWeapon)
		{
			if (this._currentWeapon)
			{
				if (this._currentWeapon.IsReloading)
					this._currentWeapon.StopReloading();
				this._currentWeapon.gameObject.SetActive(false);
			}
			this._currentWeapon = newWeapon;
			newWeapon.gameObject.SetActive(true);
			Debug.Log(Time.timeSinceLevelLoad + ": UpdateWeapon " + newWeapon.gameObject.name);
		}

		private SkeletonReference _skeletonReference;

		/// <summary>
		/// Referência do esqueleto.
		/// </summary>
		public SkeletonReference Skeleton
		{
			get
			{
				return this._skeletonReference;
			}
		}

		/// <summary>
		/// Cache do componente que cuida dos danos do personagem.
		/// </summary>
		private CharacterDamageable _damageable;

		private volatile bool _dead = false;

		/// <summary>
		/// Controlador de música.
		/// </summary>
		private MusicController _musicController;

		/// <summary>
		/// Audio que tocará os efeitos dos tiros.
		/// </summary>
		private AudioSource _audioSource;

		/// <summary>
		/// Guarda as mortes em sequencia.
		/// </summary>
		private int _killSequence = 0;

		/// <summary>
		/// Spawnpoints da fase.
		/// </summary>
		private GameObject[] _spawnPoints;

		void Start()
		{
			/// Encontra os CanvasKill globais
			this._canvasKill = GameObject.FindObjectsOfType<CanvasKill>();
			/*
			GameObject[] canvasKill = GameObject.FindGameObjectsWithTag("CanvasKill");
			this._canvasKill = new CanvasKill[canvasKill.Length];
			for (int i = 0; i < canvasKill.Length; i++)
			{
				this._canvasKill[i] = canvasKill[i].GetComponent<CanvasKill>();
			}
			*/

			/// Encontra todos os spawn points
			this._spawnPoints = GameObject.FindGameObjectsWithTag("Spawnpoint");

			/// Encontra o recurso da fonte de sangue.
			if (BloodFountain == null)
				BloodFountain = Resources.Load("BloodFountain");

			this._hudTransform = GameObject.Find("HUD").GetComponent<Canvas>().transform;
			this._audioSource = this.GetComponent<AudioSource>();
			this._musicController = this.GetComponentInChildren<MusicController>();
			if (this.photonView.isMine)
				this.gameObject.layer = Layer.Myself;
			else
				this.gameObject.layer = Layer.Players;

			Players.Set(this.photonView.owner, this);
			foreach (Collider c in this.gameObject.GetComponentsInChildren<Collider>())
				c.gameObject.layer = this.gameObject.layer;

			this._damageable = this.GetComponent<CharacterDamageable>();
			this._skeletonReference = this.GetComponentInChildren<SkeletonReference>();

			this._rootRigidbody = this.GetComponent<Rigidbody>();
			this._rootCollider = this.GetComponent<Collider>();

			this._head = this.GetComponentInChildren<HeadSprite>();

			ParticleSystem[] pSsytems = this.GetComponentsInChildren<ParticleSystem>();
			this._animator = this.GetComponent<Animator>();

			this._arsenalPlaceholder = this.GetComponentInChildren<Arsenal>();

			if (this.photonView.isMine)
			{
				this._jetpackUIBar = GameObject.Find("JetpackBar").GetComponent<Bar>();
				this._jetpackUIBar.Max = this._jetpackCapacity;

				this._ammoUIBar = GameObject.Find("AmmoBar").GetComponent<Bar>();
			}

			this._jetpackFlames = pSsytems[0];
			this._jetpackSmoke = pSsytems[1];

			/*
			this._jetpackLight = this.GetComponentInChildren<Light>();
			this._jetpackLight.gameObject.SetActive(false);
			*/

			this._jetpackFuel = this._jetpackCapacity;
			this._jetpackReloadRatio = (this._jetpackCapacity / this._jetpackReloadDuration);

			/*
			this.m_Capsule = this.GetComponent<CapsuleCollider>();
			this.m_CapsuleHeight = this.m_Capsule.height;
			this.m_CapsuleCenter = this.m_Capsule.center;
			*/

			// this.UpdateRotation();
			this._started = true;

			this.SetArsenal((Weapon.Weapons[])this.photonView.instantiationData[0]);

			this._cameraFollower = this.GetComponent<CameraFollower>();
			this._cameraFollower.enabled = this.photonView.isMine;
		}

		/// <summary>
		/// Efetua as verificações/atualizações referêntes ao Jetpack.
		/// 
		/// Se ligado com, com as condições corretas, atualiza a aceleração vertical do jogador; bem como o combustível do Jetpack.
		/// </summary>
		public void JetpackUpdate()
		{
			if (this.JetpackOn && (!this.IsGrounded) && (Time.time >= this._jetpackTime) && (this._jetpackFuel > 0f))
			{
				Vector3 v = this._rootRigidbody.velocity;
				v.y = Mathf.Min(v.y + 15f * Time.deltaTime, 4f);
				this._rootRigidbody.velocity = v;
				this._jetpackFuel = Mathf.Max(0f, this._jetpackFuel - Time.deltaTime);
				if (this.photonView.isMine)
					this._jetpackUIBar.Current = this._jetpackFuel;
			}
			else if (!this.JetpackOn)
			{
				this._jetpackFuel = Mathf.Min(this._jetpackFuel + this._jetpackReloadRatio * Time.deltaTime, this._jetpackCapacity);
				if (this.photonView.isMine)
					this._jetpackUIBar.Current = this._jetpackFuel;
			}
			else
			{
				this.JetpackOn = false;
			}
		}

		/// <summary>
		/// Inicia o pulo do jogador.
		/// </summary>
		public void Jump()
		{
			if (this._isGrounded)
			{
				this._jumpTime = Time.time;
				this._jetpackTime = this._jumpTime + 0.5f; // 0.5 segundos
				Vector3 v = this._rootRigidbody.velocity;
				v.y += this._jumpPower;
				this._rootRigidbody.velocity = v;
				this._isGrounded = false;
			}
		}

		/// <summary>
		/// Retorna se o jogador está olhando para a esquerda ou direita, dependendo da posição do mouse em relação a camera.
		/// </summary>
		protected bool IsFacingRight
		{
			get
			{
				if (this._rootRigidbody)
					return (this.transform.position.x < Camera.main.ScreenToWorldPoint(Input.mousePosition).x);
				else
					return true;
			}
		}

		/// <summary>
		/// Direção para onde a mira está apontando.
		/// </summary>
		public Vector3 AimDirection
		{
			get
			{
				return this._aimDirection;
			}
			private set
			{
				this._aimDirection = value;
				// this.AimHandler.transform.position = this.transform.position + (value * HEAD_HEIGHT) + this.AimOffset;
				this._aimAngle = -Mathf.Atan2(this._aimDirection.y, this._aimDirection.x) * Mathf.Rad2Deg;
			}
		}

		/// <summary>
		/// Retorna o ângulo da mira.
		/// </summary>
		public float AimAngle
		{
			get
			{
				return this._aimAngle;
			}
		}

		/// <summary>
		/// Atualiza para que lado o jogador está olhando.
		/// </summary>
		protected void UpdateRotation()
		{
			this.transform.rotation = Quaternion.Euler(0, 90 * (this.IsFacingRight ? 1f : -1f), 0);
		}

		/// <summary>
		/// Manipula o movimento do jogador.
		/// </summary>
		/// <param name="move">Parâmetro dos movimentos do jogador.</param>
		public void Move(Vector3 move)
		{
			this.UpdateRotation();

			this.CheckGroundStatus();

			Vector3 v = this._rootRigidbody.velocity;
			if (this._isGrounded)
			{
				v.x = move.x * this.MaxHorizontalSpeed;
			}
			else
			{
				v.x = Mathf.Clamp(v.x + move.x * this.MaxHorizontalSpeed * Time.deltaTime, -this.MaxHorizontalSpeed, this.MaxHorizontalSpeed);
			}

			/// Confirmar que o jogador nunca sairá da posição z = 0
			Vector3 pos = new Vector3(this.transform.position.x, this.transform.position.y);
			this.transform.position = pos;

			// this._rootRigidbody.velocity = v;
			this._rootRigidbody.velocity = Vector3.Lerp(this._rootRigidbody.velocity, v, 0.2f);

			this.JetpackUpdate();

			this.UpdateAnimator();
		}

		/// <summary>
		/// Atualiza a animação do jogo.
		/// 
		/// Método baseado na implementação padrão do controlador de jogador de terceira pessoa do Unity.
		/// </summary>
		void UpdateAnimator()
		{
			float amount = ((this.photonView.isMine) ? this._rootRigidbody.velocity.x : this._updatedVelocity.x ) / this.MaxHorizontalSpeed;
			this._animator.SetFloat("Forward", Mathf.Abs(amount), 0.1f, Time.deltaTime);
			// this.m_Animator.SetFloat("Turn", this.m_TurnAmount, 0.5f, Time.deltaTime);
			// this.m_Animator.SetBool("Crouch", this.m_Crouching);
			this._animator.SetBool("OnGround", this._isGrounded);
			if (!this._isGrounded)
			{
				if (this.photonView.isMine)
					this._animator.SetFloat("Jump", this._rootRigidbody.velocity.y);
				else
					this._animator.SetFloat("Jump", Mathf.Lerp(this._rootRigidbody.velocity.y, this._updatedVelocity.y, 0.2f));
			}

			float runCycle = Mathf.Repeat(this._animator.GetCurrentAnimatorStateInfo(0).normalizedTime + this._runCycleLegOffset, 1);
			float jumpLeg = (runCycle < _half ? 1 : -1) * amount;
			if (this._isGrounded)
			{
				this._animator.SetFloat("JumpLeg", jumpLeg);
			}

			/*
			// the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
			// which affects the movement speed because of the root motion.
			if (this._isGrounded && move.magnitude > 0)
				this.m_Animator.speed = m_AnimSpeedMultiplier;
			else
				// don't use that while airborne
				this.m_Animator.speed = 1;
			*/
		}
		
		/// <summary>
		/// Verifica se o jogador está tocando, ou não, o chão.
		/// </summary>
		void CheckGroundStatus()
		{
			RaycastHit hitInfo;
#if UNITY_EDITOR
			// helper to visualise the ground check ray in the scene view
			Debug.DrawLine(this.transform.position, this.transform.position + (Vector3.up * 0.1f) + (Vector3.down * this._groundCheckDistance), Color.blue);
#endif
			this._isGrounded = Physics.Raycast(this.transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, this._groundCheckDistance);
		}

		/// <summary>
		/// Verifica se o jogador é um jogador de rede e aplica as animações.
		/// </summary>
		void FixedUpdate()
		{
			if (this.photonView.isMine)
			{
				/// Código temporário apenas para a exibição da mira. Apenas por enquanto que o jogador ainda não move os braços.
				Vector3 m = Camera.main.ScreenToWorldPoint(Input.mousePosition) - this.transform.position - this.AimOffset;
				m.z = 0;
				m.Normalize();
				this.AimDirection = m;
			}
			else
			{
				//this._rigidbody.transform.position = Vector3.Lerp(this._rigidbody.transform.position, this._updatedPosition, 0.1f);
				this._rootRigidbody.transform.position = Vector3.Lerp(this._rootRigidbody.transform.position, this._updatedPosition, Time.deltaTime * 5f);
				this._rootRigidbody.transform.rotation = this._updatedRotation;
				this._rootRigidbody.velocity = this._updatedVelocity;
				this.AimDirection = Vector3.Lerp(this.AimDirection, this._updatedAimDirection, Time.deltaTime * 5f);

				this.CheckGroundStatus();
				this.UpdateAnimator();
			}
		}

		/// <summary>
		/// Caso seja local, escreve as atualizações da rede; caso seja remoto, aplica as atualizações recebidas.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="info"></param>
		void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
		{
			if (this._started)
			{
				if (stream.isWriting)
				{
					stream.SendNext(this._rootRigidbody.transform.position);
					stream.SendNext(this._rootRigidbody.transform.rotation);
					stream.SendNext(this._rootRigidbody.velocity);
					stream.SendNext(this._aimDirection);
				}
				else
				{
					this._updatedPosition = (Vector3)stream.ReceiveNext();
					this._updatedRotation = (Quaternion)stream.ReceiveNext();
					this._updatedVelocity = (Vector3)stream.ReceiveNext();
					this._updatedAimDirection = (Vector3)stream.ReceiveNext();
				}
			}
		}

		public virtual void Respawn()
		{
			this.gameObject.SetActive(true);
			this._damageable.Reset();
			this._cameraFollower.enabled = this.photonView.isMine;
			this._rootRigidbody.velocity = Vector3.zero;
			this.transform.position = this._spawnPoints[Random.Range(0, this._spawnPoints.Length)].transform.position;

			Gun g;
			foreach (Weapon w in this.Arsenal)
			{
				if (w is Gun)
				{
					g = (w as Gun);
					g.Ammo = g.CartridgeSize;
				}
			}
			
			if (this.photonView.isMine)
				this._head.SetFace(FacesManager.Die, 3);

			this._dead = false;
		}

		public virtual void Die(DeathInfo info)
		{
			if (!this._dead)
			{
				this._dead = true;
				this._cameraFollower.enabled = false;
				this.Ragdoll();
				this.BroadcastDeath(info);
				PhotonNetwork.player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable {
					{ "Deaths", this.Deaths+1 }
				});
				if (Game.Rules.RespawnMode == RespawnMode.RealTime)
				{
					this.Invoke("Respawn", Game.Rules.RespawnTime);
				}
				if (info.Assassin == this)
				{
					PhotonNetwork.player.AddScore(-1);
					Debug.Log(this + " commited suicide");
				}
				else
				{
					Debug.Log(this + " was killed by " + info.Assassin);
					info.Assassin.photonView.owner.AddScore(1);
					info.Assassin.photonView.RPC("YouKilled", info.Assassin.photonView.owner, this.photonView.owner.ID);
				}
			}
		}

		/// <summary>
		/// Método chamado quando alguém for morte por este jogador.
		/// </summary>
		/// <param name="playerId">Id do player assassinado.</param>
		[PunRPC]
		public void YouKilled(int playerId)
		{
			/// Verifica os feitos
			PhotonPlayer player = PhotonPlayer.Find(playerId);
			if (this.TimeSinceLastKill < 10f)
			{
				this._killSequence++;

				/// Exibe na tela o UI do feito.
				int max = 0, i = 0;
				bool found = false;
				foreach (CanvasKill c in this._canvasKill)
				{
					if (c.KillAmount == this._killSequence)
					{
						c.Show();
						found = true;
						break;
					}
					if (this._canvasKill[max].KillAmount < c.KillAmount)
						max = i;
					i++;
				}
				if ((!found) && (this._killSequence >= this._canvasKill[max].KillAmount))
				{
					this._canvasKill[max].Show();
				}

				/// Exibe o feito nos outros clientes.
				this.photonView.RPC("ShowAchievement", PhotonTargets.Others, new object[] {
					this.photonView.owner.ID, this._killSequence
				});
				this._head.SetFace(FacesManager.Win, 5);
			}
			else
			{
				/// Resseta o quantidade de mortes em sequencia
				this._killSequence = 1;
			}
			this._lastKillAt = Time.timeSinceLevelLoad;
		}

		[PunRPC]
		public void ShowAchievement(int playerId, int killedSequence)
		{
			PhotonPlayer player = PhotonPlayer.Find(playerId);
			if (player == null)
				FlashMessage.Popup(this._hudTransform.transform, "Algém matou " + killedSequence + " vezes em sequencia", 1f);
			else
				FlashMessage.Popup(this._hudTransform.transform, player.name + " matou " + killedSequence + " vezes em sequencia", 1f);
		}

		private void BroadcastDeath(DeathInfo info)
		{
			this.photonView.RPC("NetworkDeath", PhotonTargets.Others);
		}

		[PunRPC]
		protected void NetworkDeath()
		{
			this._cameraFollower.enabled = false;
			this.Ragdoll();
			if (Game.Rules.RespawnMode == RespawnMode.RealTime)
			{
				this.Invoke("Respawn", Game.Rules.RespawnTime);
			}
			this._dead = true;
		}

		[PunRPC]
		public void CreateProjectile1(int[] networkIds, Vector3[] directions, Vector3[] positions)
		{
			if (this.Weapon is Gun)
			{
				((Gun)this.Weapon).CreateProjectile1(directions, positions);
			}
		}

		public virtual void OnCollisionEnter(Collision collision)
		{
			if (collision.collider.GetComponent<Projectile>() != null)
			{
				GameObject bloodFountain = (GameObject)Instantiate(
					BloodFountain,
					collision.contacts[0].point,
					Quaternion.LookRotation(collision.contacts[0].point - (Vector3.Dot(collision.contacts[0].point, collision.contacts[0].normal)) * collision.contacts[0].normal, collision.contacts[0].normal)
				);
				bloodFountain.transform.SetParent(this.transform, true);
			}
		}
	}
}
