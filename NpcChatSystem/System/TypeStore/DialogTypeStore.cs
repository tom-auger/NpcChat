﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NpcChatSystem.Data.Dialog.DialogParts;

namespace NpcChatSystem.System.TypeStore
{
    public static class DialogTypeStore
    {
        public static IReadOnlyList<string> Dialogs => m_elementNames;

        private static List<string> m_elementNames = new List<string>();
        private static Dictionary<string, Type> m_elementLookup = new Dictionary<string, Type>();

        static DialogTypeStore()
        {
            ScanAssembly(Assembly.GetAssembly(typeof(IDialogElement)));
        }

        public static void ScanAssembly(Assembly ass)
        {
            foreach (TypeInfo type in ass.DefinedTypes.Where(t => t.ImplementedInterfaces.Contains(typeof(IDialogElement))))
            {
                if (type.ImplementedInterfaces.Contains(typeof(IDialogElement)))
                {
                    if (ValidateDialogConstructors(type))
                    {
                        string name = type.Name;

                        if (type.CustomAttributes.Any(a => a.AttributeType == typeof(DialogElementNameAttribute)))
                        {
                            DialogElementNameAttribute dialog = type.GetCustomAttribute<DialogElementNameAttribute>();
                            name = dialog.Name;
                        }

                        m_elementNames.Add(name);
                        m_elementLookup.Add(name, type);
                    }
                }
            }
        }

        /// <summary>
        /// Checks that the IDialogElement type can be created via reflection
        /// </summary>
        /// <param name="type">IDialogElement type</param>
        /// <returns>true if type is value</returns>
        private static bool ValidateDialogConstructors(TypeInfo type)
        {
            bool emptyConstructor = false, projectConstructor = false;

            foreach (ConstructorInfo constructor in type.DeclaredConstructors)
            {
                if (!constructor.IsPublic) continue;

                ParameterInfo[] parameters = constructor.GetParameters();
                if (parameters.Length == 0)
                    emptyConstructor = true;
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(NpcChatProject))
                    projectConstructor = true;
                else
                {
                    bool defaultTypes = true;
                    bool containsProject = false;
                    foreach (ParameterInfo param in parameters)
                    {
                        if (param.ParameterType == typeof(NpcChatProject))
                        {
                            if (!containsProject) containsProject = true;
                            else
                            {
                                //todo add warning log here once log library is in place
                                //todo add test case for an element with 2 npc project parameters as this is a strange edge case...

                                //a valid constructor should only require one npcProject
                                containsProject = false;
                                break;
                            }
                        }
                        else if (!param.HasDefaultValue) defaultTypes = false;
                    }

                    if (containsProject && defaultTypes)
                    {
                        projectConstructor = true;
                    }
                }
            }

            if (emptyConstructor || projectConstructor) return true;

            //todo log msg once logging is in place
            string msg =
                $"Failed to add Dialog Element type '{type.FullName}' as it does not have any supported constructors! At least one of the following public constructors must be implemented:" +
                $"    public {type.Name}()..." +
                $"    public {type.Name}({nameof(NpcChatProject)} project)..." +
                $"Variation of this is possible but all none '{nameof(NpcChatProject)}' parameters must have a default value!";

            return false;
        }

        public static IDialogElement CreateDialogElement(string dialogName, NpcChatProject project = null)
        {
            if (m_elementLookup.ContainsKey(dialogName))
            {
                Type type = m_elementLookup[dialogName];

                if (type.GetConstructors().Any(c => c.GetParameters().Length == 0))
                {
                    return Activator.CreateInstance(type) as IDialogElement;
                }
                else
                {
                    foreach (ConstructorInfo constructor in type.GetConstructors())
                    {
                        if (!constructor.IsPublic) continue;

                        ParameterInfo[] parameters = constructor.GetParameters();
                        List<object> constructorParameters = new List<object>(parameters.Length);

                        bool defaultTypes = true;
                        bool containsProject = false;
                        foreach (ParameterInfo param in parameters)
                        {
                            if (param.ParameterType == typeof(NpcChatProject))
                            {
                                if (!containsProject)
                                {
                                    constructorParameters.Add(project);
                                    containsProject = true;
                                }
                                else
                                {
                                    containsProject = false;
                                    break;
                                }
                            }
                            else if(!param.HasDefaultValue)
                            {
                                defaultTypes = false;
                                break;
                            }
                            else
                            {
                                constructorParameters.Add(param.DefaultValue);
                            }
                        }

                        if (defaultTypes && containsProject)
                        {
                            return constructor.Invoke(constructorParameters.ToArray()) as IDialogElement;
                        }
                    }
                }

                return Activator.CreateInstance(type, project) as IDialogElement;
            }

            //todo dialog creation error once logging library is in
            return null;
        }

        public static T CreateDialogElement<T>(string dialogName, NpcChatProject project = null) where T : class, IDialogElement
        {
            return CreateDialogElement(dialogName, project) as T;
        }
    }
}
