﻿using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Event.Default;
using NHibernate.Hql.Util;
using NHibernate.Tool.hbm2ddl;
using NLog;
using System;
using System.Threading;

namespace Teltec.Backup.App.DAO.NHibernate
{
	public static class NHibernateHelper
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		// TODO(jweyrich): When to dispose it?
		private static ISessionFactory _sessionFactory;

		// TODO(jweyrich): When to dispose it?
		private static Configuration _configuration;

		// TODO(jweyrich): When to dispose it?
		private static readonly ThreadLocal<ISession> _sessions = new ThreadLocal<ISession>();

		public static ISession GetSession()
		{
			//
			// NOTES:
			// 1. The ISession is not threadsafe! Never access the same ISession in two concurrent threads.
			// 2. 
			//

			if (!_sessions.IsValueCreated)
			{
				logger.Debug("### Opening a new ISession");
				// Open a new NHibernate session
				_sessions.Value = SessionFactory.OpenSession();
				//_sessions.Value.FlushMode = FlushMode.Never;
			}

			return _sessions.Value;
		}

		public static bool IsTransient(ISession session, object obj)
		{
			ISessionFactoryImplementor sessionFactoryImpl = session.SessionFactory as ISessionFactoryImplementor;
			var persister = new SessionFactoryHelper(sessionFactoryImpl)
				.RequireClassPersister(obj.GetType().FullName);
			bool? yes = persister.IsTransient(obj, (ISessionImplementor)session);
			return yes ?? default(bool);
		}

		public static T Unproxy<T>(object instance) where T : class
		{
			return GetSession().GetSessionImplementation().PersistenceContext.Unproxy(instance) as T;
		}

		public static ISessionFactory SessionFactory
		{
			get
			{
				if (_sessionFactory == null)
				{
					//Create the session factory
					_sessionFactory = Configuration.BuildSessionFactory();
				}
				return _sessionFactory;
			}
		}

		public static Configuration Configuration
		{
			get
			{
				if (_configuration == null)
				{
					//Create the nhibernate configuration
					_configuration = CreateConfiguration();
				}
				return _configuration;
			}
		}

		private static Configuration CreateConfiguration()
		{
			FluentConfiguration fluentConfig = Fluently.Configure();
			fluentConfig.Database(SQLiteConfiguration.Standard.UsingFile("database.sqlite3"));
			fluentConfig.Diagnostics(diag => diag.Enable().OutputToConsole());

			// Mappings.
			fluentConfig.Mappings(m => m.FluentMappings
				.Add<StorageAccountMap>()
				.Add<AmazonS3AccountMap>()
				.Add<BackupPlanMap>()
				.Add<BackupPlanSourceEntryMap>()
				.Add<BackupMap>()
				.Add<BackupPlanFileMap>()
				.Add<BackupedFileMap>()
			);

			Configuration config = fluentConfig.BuildConfiguration();

			// Register interceptors.
			config.SetInterceptor(new NHibernateAuditInterceptor());

			// Register listeners.
			config.AppendListeners(ListenerType.Load, new ILoadEventListener[] {
				new NHibernateLoadListener(),
			});
			config.AppendListeners(ListenerType.PostLoad, new IPostLoadEventListener[] {
				new NHibernatePersistentEntityListener(),
			});
			config.AppendListeners(ListenerType.Save, new ISaveOrUpdateEventListener[] {
				new NHibernatePersistentEntityListener(),
			});
			config.AppendListeners(ListenerType.Update, new ISaveOrUpdateEventListener[] {
				new NHibernatePersistentEntityListener(),
			});

			UpdateSchema(config);
			ValidateSchema(config);

			return config;
		}

		private static bool ValidateSchema(Configuration config)
		{
			SchemaValidator validator = new SchemaValidator(config);
			try
			{
				validator.Validate();
				validator = null;
				return true;
			}
			catch (Exception ex)
			{
				logger.Fatal("Schema validation error", ex);
			}
			finally
			{
				validator = null;
			}
			return false;
		}

		private static void UpdateSchema(Configuration config)
		{
			SchemaUpdate schema = new SchemaUpdate(config);
			const bool useStdOut = true;
			const bool doUpdate = true;
			schema.Execute(useStdOut, doUpdate);
			schema = null;
		}

		public static string IdentifierPropertyName(Type type)
		{
			return SessionFactory.GetClassMetadata(type).IdentifierPropertyName;
		}

		public static string EntityName(Type type)
		{
			return NHibernateUtil.Entity(type).Name;
		}
	}
}