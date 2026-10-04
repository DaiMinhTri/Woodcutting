using UnityEngine;

namespace Woodcutting;

internal class HpDisplay : MonoBehaviour
{
	private Camera? mainCamera;
	private GUIStyle? style;
	private string labelText = "";
	private bool show;

	private void Update()
	{
		show = false;

		if (Woodcutting.showHpOnAim.Value != Woodcutting.Toggle.On)
		{
			return;
		}

		if (Woodcutting.GetWoodCuttingSkillLevel() < Woodcutting.hpDisplaySkillLevel.Value)
		{
			return;
		}

		Player player = Player.m_localPlayer;
		if (!player || player.IsDead())
		{
			return;
		}

		if (!mainCamera)
		{
			mainCamera = Camera.main;
		}

		if (!mainCamera)
		{
			return;
		}

		if (!Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out RaycastHit hit, 100f))
		{
			return;
		}

		float max;
		ZNetView? nview;
		GameObject target;

		TreeBase tree = hit.collider.GetComponentInParent<TreeBase>();
		if (tree)
		{
			max = tree.m_health;
			nview = tree.GetComponent<ZNetView>();
			target = tree.gameObject;
		}
		else
		{
			TreeLog log = hit.collider.GetComponentInParent<TreeLog>();
			if (log)
			{
				max = log.m_health;
				nview = log.GetComponent<ZNetView>();
				target = log.gameObject;
			}
			else
			{
				Destructible destructible = hit.collider.GetComponentInParent<Destructible>();
				if (!destructible || !StumpHp.IsStump(StumpHp.CleanName(destructible.gameObject.name)))
				{
					return;
				}

				max = destructible.m_health;
				nview = destructible.GetComponent<ZNetView>();
				target = destructible.gameObject;
			}
		}

		float current = max;
		if (nview)
		{
			ZDO? zdo = nview.GetZDO();
			if (zdo != null)
			{
				current = zdo.GetFloat("health", max);
			}
		}

		if (current > max)
		{
			current = max;
		}

		labelText = $"{StumpHp.CleanName(target.name)}  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
		show = true;
	}

	private void OnGUI()
	{
		if (!show)
		{
			return;
		}

		if (style == null)
		{
			style = new GUIStyle(GUI.skin.label)
			{
				fontSize = 16,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter
			};
		}

		float x = (Screen.width / 2f) - 150f;
		float y = (Screen.height / 2f) - 60f;
		Rect rect = new(x, y, 300f, 30f);

		style.normal.textColor = Color.black;
		GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), labelText, style);
		style.normal.textColor = Color.white;
		GUI.Label(rect, labelText, style);
	}
}
