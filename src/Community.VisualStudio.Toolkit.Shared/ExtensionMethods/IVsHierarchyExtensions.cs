﻿using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ComponentModelHost;

namespace Microsoft.VisualStudio.Shell.Interop
{
    /// <summary>
    /// Extension methods for the <see cref="IVsHierarchyExtensions"/> interface.
    /// </summary>
    public static class IVsHierarchyExtensions
    {
        /// <summary>
        /// Tries to get a property from a hierarchy item.
        /// </summary>
        /// <remarks>
        /// Inspired by https://github.com/dotnet/roslyn/blob/main/src/VisualStudio/Core/Def/Implementation/ProjectSystem/Extensions/IVsHierarchyExtensions.cs
        /// </remarks>
        public static bool TryGetItemProperty<T>(this IVsHierarchy hierarchy, uint itemId, int propertyId, out T? value)
        {
            return HierarchyUtilities.TryGetHierarchyProperty<T>(hierarchy, itemId, propertyId, out value);
        }

        /// <summary>
        /// Converts a <see cref="IVsHierarchy"/> to a <see cref="IVsHierarchyItem"/>.
        /// </summary>
        /// <returns>Returns <see langword="null"/> if unable to get the hierarchy item.</returns>
        public static async Task<IVsHierarchyItem?> ToHierarcyItemAsync(this IVsHierarchy hierarchy, uint itemId)
        {
            if (hierarchy == null) return null;
                
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            IComponentModel components = await VS.GetRequiredServiceAsync<SComponentModel, IComponentModel>();

            IVsHierarchyItemManager? manager = components.GetService<IVsHierarchyItemManager>();

            manager.TryGetHierarchyItem(hierarchy, itemId, out IVsHierarchyItem? item);
            return item;
        }

        /// <summary>
        /// Converts an IVsHierarchy to a Project.
        /// </summary>
        public static Project? ToProject(this IVsHierarchy hierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (hierarchy == null)
            {
                throw new ArgumentNullException(nameof(hierarchy));
            }

            hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out var obj);

            return obj as Project;
        }

        /// <summary>
        /// Converts an IVsHierarchy to a ProjectItem.
        /// </summary>
        public static ProjectItem? ToProjectItem(this IVsHierarchy hierarchy, uint itemId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (hierarchy == null)
            {
                throw new ArgumentNullException(nameof(hierarchy));
            }

            hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out var obj);

            return obj as ProjectItem;
        }

        /// <summary>
        /// Returns whether the specified <see cref="IVsHierarchy"/> is an 'SDK' style project.
        /// </summary>
        /// <param name="hierarchy"></param>
        /// <returns></returns>
        public static bool IsSdkStyleProject(this IVsHierarchy hierarchy)
        {
            if (hierarchy == null)
            {
                throw new ArgumentNullException(nameof(hierarchy));
            }

            return hierarchy.IsCapabilityMatch("CPS");
        }

        /// <summary>Check what kind the project is.</summary>
        /// <param name="hierarchy">The hierarchy instance to check.</param>
        /// <param name="typeGuid">Use the <see cref="ProjectTypes"/> list of strings.</param>
        public static bool IsProjectOfType(this IVsHierarchy hierarchy, string typeGuid)
        {
            if (hierarchy == null)
            {
                throw new ArgumentNullException(nameof(hierarchy));
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            if (hierarchy is IVsAggregatableProject aggregatable)
            {
                if (ErrorHandler.Succeeded(aggregatable.GetAggregateProjectTypeGuids(out var types)))
                {
                    var guid = new Guid(typeGuid);

                    foreach (var type in types.Split(';'))
                    {
                        if (Guid.TryParse(type, out Guid identifier) && guid.Equals(identifier))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the <see cref="IVsSharedAssetsProject"/> for the <see cref="IVsHierarchy"/>.
        /// </summary>
        /// <param name="hierarchy"></param>
        public static IVsSharedAssetsProject? GetSharedAssetsProject(this IVsHierarchy hierarchy)
        {
            if (hierarchy == null)
            {
                throw new ArgumentNullException(nameof(hierarchy));
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            HierarchyUtilities.TryGetHierarchyProperty<IVsSharedAssetsProject>(
                hierarchy,
                (uint)VSConstants.VSITEMID.Root,
                (int)__VSHPROPID7.VSHPROPID_SharedAssetsProject,
                out IVsSharedAssetsProject? sharedAssetsProject);

            return sharedAssetsProject;
        }

        /// <summary>
        /// Returns whether the <see cref="IVsHierarchy"/> is a shared project.
        /// </summary>
        /// <param name="hierarchy"></param>
        public static bool IsSharedProject(this IVsHierarchy hierarchy)
        {
            if (hierarchy == null)
            {
                throw new ArgumentNullException(nameof(hierarchy));
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            return hierarchy.GetSharedAssetsProject() != null;
        }

        /// <summary>
        /// Tries to set a build property on the project.
        /// </summary>
        public static bool TrySetBuildProperty(this IVsHierarchy hierarchy, string name, string value, _PersistStorageType storageType = _PersistStorageType.PST_USER_FILE)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (hierarchy is IVsBuildPropertyStorage storage)
            {
                // Store the build property in the user file instead of the project
                // file, because we don't want to affect the real project file.
                storage.SetPropertyValue(name, "", (uint)storageType, value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get the specified build property from the project.
        /// </summary>
        public static bool TryGetBuildProperty(this IVsHierarchy hierarchy, string name, out string? value, _PersistStorageType storageType = _PersistStorageType.PST_USER_FILE)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            value = null;

            if (hierarchy is IVsBuildPropertyStorage storage)
            {
                return storage.GetPropertyValue(name, "", (uint)storageType, out value) == VSConstants.S_OK;
            }

            return false;
        }
    }
}
