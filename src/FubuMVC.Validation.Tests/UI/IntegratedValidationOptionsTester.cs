﻿using System;
using System.Linq.Expressions;
using FubuCore;
using FubuCore.Reflection;
using FubuMVC.Core.Http;
using FubuMVC.Core.Registration;
using FubuMVC.Core.Registration.Querying;
using FubuMVC.Core.UI.Forms;
using FubuMVC.Core.Urls;
using FubuMVC.Validation.UI;
using FubuTestingSupport;
using FubuValidation;
using FubuValidation.Fields;
using HtmlTags;
using NUnit.Framework;

namespace FubuMVC.Validation.Tests.UI
{
	[TestFixture]
	public class IntegratedValidationOptionsTester
	{
		private BehaviorGraph theGraph;

		[SetUp]
		public void SetUp()
		{
			theGraph = BehaviorGraph.BuildFrom(x =>
			{
				x.Actions.IncludeType<ValidationOptionsEndpoint>();
				x.Import<FubuMvcValidation>();
			});
		}

		private ValidationOptions theOptions
		{
			get { return ValidationOptions.For(createRequest()); }
		}

		private FieldOptions field(Expression<Func<ValidationOptionsTarget, object>> expression, ValidationMode mode, params FieldRuleOptions[] rules)
		{
			return new FieldOptions
			{
				field = expression.ToAccessor().Name,
				mode = mode.Mode,
				rules = rules
			};
		}

		private FieldRuleOptions rule<T>(ValidationMode mode)
			where T : IFieldValidationRule
		{
			return new FieldRuleOptions
			{
				rule = RuleAliases.AliasFor(typeof(T)),
				mode = mode.Mode
			};
		}

		[Test]
		public void verify_the_fields()
		{
			theOptions.fields.ShouldHaveTheSameElementsAs(

				field(x => x.Default, ValidationMode.Live),
				field(x => x.LiveAttribute, ValidationMode.Live),

				field(x => x.LiveRule, ValidationMode.Live, 
					rule<EmailFieldRule>(ValidationMode.Live),
					rule<RequiredFieldRule>(ValidationMode.Triggered)),
				
				field(x => x.TriggeredAttribute, ValidationMode.Triggered),
				field(x => x.TriggeredRule, ValidationMode.Triggered)
			
			);
		}

		private FormRequest createRequest()
		{
			var rules = new AccessorRules();
			var overrides = new ValidationOptionsTargetOverrides().As<IAccessorRulesRegistration>();
			
			overrides.AddRules(rules);

			var services = new InMemoryServiceLocator();
			services.Add<IChainResolver>(new ChainResolutionCache(new TypeResolver(), theGraph));
			services.Add(rules);
			services.Add<IChainUrlResolver>(new ChainUrlResolver(new StandInCurrentHttpRequest()));
			services.Add<ITypeResolver>(new TypeResolver());
			services.Add<ITypeDescriptorCache>(new TypeDescriptorCache());

			var graph = ValidationGraph.BasicGraph();
			graph.Fields.FindWith(new AccessorRulesFieldSource(rules));

			services.Add(graph);

			var request = new FormRequest(new ChainSearch { Type = typeof(ValidationOptionsTarget) }, new ValidationOptionsTarget());
			request.Attach(services);
			request.ReplaceTag(new FormTag("test"));

			return request;
		}

		public class ValidationOptionsEndpoint
		{
			public ValidationOptionsTarget post_target(ValidationOptionsTarget target)
			{
				return target;
			}
		}

		public class ValidationOptionsTarget
		{
			public string Default { get; set; }

			[LiveValidation]
			public string LiveAttribute { get; set; }
			public string LiveRule { get; set; }

			[TriggeredValidation]
			public string TriggeredAttribute { get; set; }
			public string TriggeredRule { get; set; }
		}

		public class ValidationOptionsTargetOverrides : OverridesFor<ValidationOptionsTarget>
		{
			public ValidationOptionsTargetOverrides()
			{
				Property(x => x.LiveRule).LiveValidation();
				Property(x => x.TriggeredRule).TriggeredValidation();

				Property(x => x.LiveRule).Email();
				// Override the default mode for a specific property/rule combination
				Property(x => x.LiveRule).Required(ValidationMode.Triggered);
			}
		}
	}
}