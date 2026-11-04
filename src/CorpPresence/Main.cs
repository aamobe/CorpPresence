using System;
using System.Linq;
using CorpPresence.Malice;
using CorpPresence.Runtime;
using CorpPresence.UI;
using HarmonyLib;
using UnityEngine;

namespace CorpPresence;

public class Main : IMod
{
	public const string Version = "0.9.5";

	public const string DisplayName = "CorpPresence";

	private Harmony _harmony;

	private GameObject _host;

	private GameObject _bootstrapObject;

	private ResourceBootstrap _bootstrap;

	private readonly RuntimeLifetime _runtime = new RuntimeLifetime((Exception error) =>
	{
		ModLog.Warn("卸载公司运行时资源失败：" + error.Message);
	});

	public void Init(ModManifest manifest)
	{
		_harmony = new Harmony("mod." + manifest.ID);
		Settings.Init(manifest.ID);
	}

	public void OnEnable()
	{
		if (_runtime.Start(EnableCore))
		{
			_bootstrap.Tick();
		}
	}

	private void EnableCore()
	{
		_runtime.Own(GameResources.Begin, GameResources.Reset);
		_runtime.Own(ItemRegistry.Install, ItemRegistry.Uninstall);
		_runtime.Own(() =>
		{
			Hooks.InstallCore(_harmony);
		}, () =>
		{
			_harmony?.UnpatchAll(_harmony.Id);
		});
		_runtime.Own(CreateBootstrap, DestroyBootstrap);
	}

	private void CompleteStartup()
	{
		_runtime.Continue(EnableResources);
		DestroyBootstrap();
	}

	private void EnableResources()
	{
		_runtime.Own(SilverBlood.InstallResources, SilverBlood.UninstallResources);
		_runtime.Own(Tobacco.RegisterPlant, Tobacco.UnregisterPlant);
		_runtime.Own(() => Hooks.InstallResources(_harmony, AbortWaterStartup), WaterHooks.Uninstall);
		_runtime.Own(Hooks.InstallEvents, Hooks.UninstallEvents);
		_runtime.Own(CorpPresence.Malice.Malice.Install, CorpPresence.Malice.Malice.Uninstall);
		_runtime.Own(Noop, CatalogPricing.Reset);
		_runtime.Own(Noop, Crates.Reset);
		ItemRegistry.Activate();
		_runtime.Own(Noop, Tex.ReleaseUi);
		_runtime.Own(Noop, Theme.ResetCachedFont);
		_runtime.Own(Noop, Portraits.ResetSceneReferences);
		_runtime.Own(Noop, Notebook.Destroy);
		_runtime.Own(Noop, InputShield.Destroy);
		_runtime.Own(Noop, CompanyWindow.ReleaseUi);
		_runtime.Own(CreateHost, DestroyHost);
		ModLog.Info("公司增强 0.9.5 资源阶段已加载（人物：" + string.Join("、", Engine.All.Select((Character c) => c.Id)) + "）");
	}

	public void OnDisable()
	{
		_runtime.Stop();
	}

	private void AbortWaterStartup(Exception error)
	{
		_runtime.Stop();
		ModLog.Warn("[水质身份兼容] 安装失败，已执行公司运行时回滚；如有撤销失败请以完整日志为准。保留原版容器初始化结果：" + error);
	}

	private static void Noop()
	{
	}

	private void CreateBootstrap()
	{
		_bootstrapObject = new GameObject("CorpPresence_Bootstrap");
		UnityEngine.Object.DontDestroyOnLoad(_bootstrapObject);
		_bootstrapObject.hideFlags = HideFlags.HideAndDontSave;
		_bootstrap = _bootstrapObject.AddComponent<ResourceBootstrap>();
		_bootstrap.Configure(CompleteStartup, _runtime.Stop);
	}

	private void DestroyBootstrap()
	{
		_bootstrap?.Detach();
		_bootstrap = null;
		if (!(_bootstrapObject == null))
		{
			_bootstrapObject.SetActive(value: false);
			UnityEngine.Object.Destroy(_bootstrapObject);
			_bootstrapObject = null;
		}
	}

	private void CreateHost()
	{
		_host = new GameObject("CorpPresence_Host");
		UnityEngine.Object.DontDestroyOnLoad(_host);
		_host.hideFlags = HideFlags.HideAndDontSave;
		_host.AddComponent<Host>();
	}

	private void DestroyHost()
	{
		if (!(_host == null))
		{
			_host.SetActive(value: false);
			UnityEngine.Object.Destroy(_host);
			_host = null;
		}
	}
}


