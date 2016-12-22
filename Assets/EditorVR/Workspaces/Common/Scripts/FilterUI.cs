﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Utilities;

public class FilterUI : MonoBehaviour
{
	private const string kAllText = "All";

	public Text summaryText { get { return m_SummaryText; } }
	[SerializeField]
	private Text m_SummaryText;

	public Text descriptionText { get { return m_DescriptionText; } }
	[SerializeField]
	private Text m_DescriptionText;

	[SerializeField]
	private GameObject m_VisibilityButton;

	[SerializeField]
	private GameObject m_SummaryButton;

	[SerializeField]
	private RectTransform m_ButtonList;

	[SerializeField]
	private GameObject m_ButtonPrefab;

	[SerializeField]
	private Color m_ActiveColor;

	[SerializeField]
	private Color m_DisableColor;

	private FilterButtonUI[] m_VisibilityButtons;

	public List<string> filterTypes
	{
		set
		{
			// Clean up old buttons
			if (m_VisibilityButtons != null)
				foreach (var button in m_VisibilityButtons)
					U.Object.Destroy(button.gameObject);


			m_FilterTypes = value;
			m_FilterTypes.Sort();
			m_FilterTypes.Insert(0, kAllText);

			// Generate new button list
			m_VisibilityButtons = new FilterButtonUI[m_FilterTypes.Count];
			for (int i = 0; i < m_VisibilityButtons.Length; i++)
			{
				var button = U.Object.Instantiate(m_ButtonPrefab, m_ButtonList, false).GetComponent<FilterButtonUI>();
				m_VisibilityButtons[i] = button;

				button.button.onClick.AddListener(() =>
				{
					OnFilterClick(button);
				});

				button.text.text = m_FilterTypes[i];
			}
		}
	}

	private List<string> m_FilterTypes;

	public string searchQuery { get { return m_SearchQuery; } }
	private string m_SearchQuery = string.Empty;

	public void SetListVisibility(bool show)
	{
		if (show)
		{
			m_ButtonList.gameObject.SetActive(true);
			m_VisibilityButton.SetActive(false);
			m_SummaryButton.SetActive(false);
		}
		else
		{
			m_ButtonList.gameObject.SetActive(false);
			m_VisibilityButton.SetActive(true);
			m_SummaryButton.SetActive(true);
		}
	}

	public void OnFilterClick(FilterButtonUI clickedButton)
	{
		for (int i = 0; i < m_VisibilityButtons.Length; i++)
			if (clickedButton == m_VisibilityButtons[i])
				m_SearchQuery = i == 0 ? string.Empty : "t:" + m_FilterTypes[i];

		foreach (FilterButtonUI button in m_VisibilityButtons)
		{
			if (button == clickedButton)
				button.color = m_ActiveColor;
			else
				button.color = m_SearchQuery.Contains("t:") ? m_DisableColor : m_ActiveColor;
		}

		if (clickedButton.text.text.Equals(kAllText))
		{
			m_SummaryText.text = clickedButton.text.text;
			m_DescriptionText.text = "All objects are visible";
		}
		else
		{
			m_SummaryText.text = clickedButton.text.text + "s";
			m_DescriptionText.text = "Only " + m_SummaryText.text + " are visible";
		}
	}

	public static bool TestFilter(string query, string type)
	{
		var pieces = query.Split(':');
		if (pieces.Length > 1)
		{
			if (pieces[1].StartsWith(type))
				return true;
		}
		else
		{
			return true;
		}
		return false;
	}
}