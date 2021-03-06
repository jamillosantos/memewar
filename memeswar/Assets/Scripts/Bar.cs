﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Implementação básica das barras da HUD.
/// </summary>
public class Bar : MonoBehaviour
{
	/// <summary>
	/// Gradiente da barra.
	/// </summary>
	public Gradient Gradient;

	/// <summary>
	/// Valor mínimo.
	/// </summary>
	public float Min = 0;

	/// <summary>
	/// Valor máximo;
	/// </summary>
	public float Max = 100;

	private Image _image;
	private Image _backgroundImage;

	private float _current;

	private Text _text;

	private Vector2 _sizeTarget;

	/// <summary>
	/// Valor atual da barra.
	/// </summary>
	public float Current
	{
		get
		{
			return this._current;
		}
		set
		{
			/// Seta o tamanho da barra de acordo com o valor atual.
			this._current = value;
			float ratio = Mathf.Clamp((this._current - this.Min) / (this.Max - this.Min), 0f, 1f);

			this._sizeTarget = new Vector2(this._backgroundImage.rectTransform.rect.width * ratio, this._backgroundImage.rectTransform.rect.height);
			this._image.rectTransform.sizeDelta = this._sizeTarget;
			// this._image.color = this.Gradient.Evaluate(ratio);
			if (this._text)
				this._text.text = Mathf.Round(this._current).ToString();
		}
	}

	/// <summary>
	/// Seta as referências iniciais do componente.
	/// </summary>
	void Start()
	{
		this._text = this.GetComponentInChildren<Text>();
		Image[] images = this.GetComponentsInChildren<Image>();
		foreach (Image image in images)
		{
			switch (image.gameObject.name)
			{
				case "Background":
					this._backgroundImage = image;
					break;
				case "Bar":
					this._image = image;
					break;
			}
		}
		this._sizeTarget = this._image.rectTransform.sizeDelta;
		this.Current = this.Max;
	}
}
