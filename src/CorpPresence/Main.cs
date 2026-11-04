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
		bool started;
		try { started = _runtime.Start(EnableCore); }
		catch (Exception error)
		{
			ReportStartupRollback(error);
			throw;
		}
		if (started)
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
		_runtime.Own(() => Hooks.InstallResources(_harmony, ReportWaterRollback), WaterHooks.Uninstall);
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

	private static void ReportWaterRollback(Exception error)
	{
		ModLog.Warn("[水质身份兼容] 安装失败，已尝试撤销本轮水质补丁。公司物品注册保留：" + error);
		RollbackNotice.Queue(fullRollback: false);
	}

	private void ReportStartupRollback(Exception error)
	{
		_runtime.Stop();
		ModLog.Error("公司增强初始化失败，已执行运行时回滚：" + error);
		RollbackNotice.Queue(fullRollback: true);
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
		_bootstrap.Configure(CompleteStartup, ReportStartupRollback);
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


