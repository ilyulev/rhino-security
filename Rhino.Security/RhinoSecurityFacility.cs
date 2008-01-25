namespace Rhino.Security
{
    using Castle.Components.Validator;
    using Castle.Core;
    using Castle.Core.Interceptor;
    using Castle.MicroKernel.Facilities;
    using Commons;

    /// <summary>
    /// Setup everything necessary for Rhino Security to function
    /// </summary>
    public class RhinoSecurityFacility : AbstractFacility
    {
        ///<summary>
        ///The custom initialization for the Facility.
        ///</summary>
        protected override void Init()
        {
			Kernel
                .AddComponentEx<AddCachingInterceptor>()
                .AddComponentEx<IAuthorizationService>()
                    .WithImplementation<AuthorizationService>()
                    .WithInterceptors(new InterceptorReference(typeof(AddCachingInterceptor))).Anywhere
                .AddComponentEx<IAuthorizationRepository>()
                    .WithImplementation<AuthorizationRepository>()
                    .WithInterceptors(new InterceptorReference(typeof(AddCachingInterceptor))).Anywhere
                .AddComponentEx<IPermissionsBuilderService>()
                    .WithImplementation<PermissionsBuilderService>()
                    .WithInterceptors(new InterceptorReference(typeof(AddCachingInterceptor))).Anywhere
                .AddComponentEx<IPermissionsService>()
                    .WithImplementation<PermissionsService>()
                    .WithInterceptors(new InterceptorReference(typeof(AddCachingInterceptor))).Anywhere
            .Register();

            if (Kernel.HasComponent(typeof(IValidatorRegistry)) == false)
            {
                Kernel
                    .AddComponentEx<IValidatorRegistry>()
                    .WithImplementation<CachedValidationRegistry>()
                    .Register();
            }

            if (Kernel.HasComponent(typeof(ValidatorRunner)) == false)
            {
                Kernel.AddComponentEx<ValidatorRunner>().Register();
            }
        }
    }
}