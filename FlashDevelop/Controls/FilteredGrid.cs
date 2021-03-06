﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using PluginCore;

namespace FlashDevelop.Controls
{
    /// <summary>
    /// This class overrides the standard PropertyGrid provided by Microsoft.
    /// It also allows to hide (or filter) the properties of the SelectedObject displayed by the PropertyGrid.
    /// </summary>
    public class FilteredGrid : PropertyGridEx
    {
        /// <summary>
        /// Contain a reference to the collection of properties to show in the parent PropertyGrid.
        /// </summary>
        readonly List<PropertyDescriptor> m_PropertyDescriptors = new List<PropertyDescriptor>();

        /// <summary>
        /// Contain a reference to the array of properties to display in the PropertyGrid.
        /// </summary>
        AttributeCollection m_HiddenAttributes = null, m_BrowsableAttributes = null;

        /// <summary>
        /// Contain references to the arrays of properties or categories to hide.
        /// </summary>
        string[] m_BrowsableProperties = null, m_HiddenProperties = null;

        /// <summary>
        /// Contain a reference to the wrapper that contains the object to be displayed into the PropertyGrid.
        /// </summary>
        ObjectWrapper m_Wrapper = null;

        /// <summary>
        /// Public constructor.
        /// </summary>
        public FilteredGrid() => base.SelectedObject = m_Wrapper;

        /// <summary>
        /// 
        /// </summary>
        public new AttributeCollection BrowsableAttributes 
        {
            get => m_BrowsableAttributes;
            set
            {
                if (value == m_BrowsableAttributes) return;
                m_HiddenAttributes = null;
                m_BrowsableAttributes = value;
                RefreshProperties();
            }
        }

        /// <summary>
        /// Get or set the categories to hide.
        /// </summary>
        public AttributeCollection HiddenAttributes 
        {
            get => m_HiddenAttributes;
            set
            {
                if (value == m_HiddenAttributes) return;
                m_HiddenAttributes = value;
                m_BrowsableAttributes = null;
                RefreshProperties();
            }
        }

        /// <summary>
        /// Get or set the properties to show.
        /// </summary>
        public string[] BrowsableProperties 
        {
            get => m_BrowsableProperties;
            set
            {
                if (value == m_BrowsableProperties) return;
                m_BrowsableProperties = value;
                RefreshProperties();
            }
        }

        /// <summary>
        /// Get or set the properties to hide.
        /// </summary>
        public string[] HiddenProperties 
        {
            get => m_HiddenProperties;
            set
            {
                if (value == m_HiddenProperties) return;
                m_HiddenProperties = value;
                RefreshProperties();
            }
        }

        /// <summary>
        /// Overwrite the PropertyGrid.SelectedObject property.
        /// </summary>
        public new object SelectedObject 
        {
            get => m_Wrapper != null ? ((ObjectWrapper)base.SelectedObject).SelectedObject : null;
            set 
            {
                if (value != null)
                {
                    if (m_Wrapper is null) 
                    {
                        m_Wrapper = new ObjectWrapper(value);
                        RefreshProperties();
                    }
                    else if (m_Wrapper.SelectedObject != value) 
                    {
                        var needrefresh = value.GetType() != m_Wrapper.SelectedObject.GetType();
                        m_Wrapper.SelectedObject = value;
                        if (needrefresh) RefreshProperties();
                    }
                    // Set the list of properties to the wrapper.
                    m_Wrapper.PropertyDescriptors = m_PropertyDescriptors;
                    // Link the wrapper to the parent PropertyGrid.
                    base.SelectedObject = m_Wrapper;
                }
                else
                {
                    m_Wrapper = null;
                    base.SelectedObject = null;
                }
            }
        }

        /// <summary>
        /// Called when the browsable properties have changed.
        /// </summary>
        void OnBrowsablePropertiesChanged() 
        {
        }

        /// <summary>
        /// Build the list of the properties to be displayed in the PropertyGrid, following the filters defined the Browsable and Hidden properties.
        /// </summary>
        void RefreshProperties() 
        {
            if (m_Wrapper is null) return;
            // Clear the list of properties to be displayed.
            m_PropertyDescriptors.Clear();
            // Check whether the list is filtered 
            if (!m_BrowsableAttributes.IsNullOrEmpty())
            {
                // Add to the list the attributes that need to be displayed.
                foreach(Attribute attribute in m_BrowsableAttributes) ShowAttribute(attribute);
            } 
            else 
            {
                // Fill the collection with all the properties.
                var originalpropertydescriptors = TypeDescriptor.GetProperties(m_Wrapper.SelectedObject);
                foreach(PropertyDescriptor propertydescriptor in originalpropertydescriptors) m_PropertyDescriptors.Add(propertydescriptor);
                // Remove from the list the attributes that mustn't be displayed.
                if(m_HiddenAttributes != null) foreach(Attribute attribute in m_HiddenAttributes) HideAttribute(attribute);
            }
            // Get all the properties of the SelectedObject
            var allproperties = TypeDescriptor.GetProperties(m_Wrapper.SelectedObject);
            // Hide if necessary, some properties
            if (!m_HiddenProperties.IsNullOrEmpty()) 
            {
                // Remove from the list the properties that mustn't be displayed.
                foreach(var propertyname in m_HiddenProperties)
                {
                    try 
                    {
                        // Remove from the list the property
                        HideProperty(allproperties[propertyname]);
                    } 
                    catch(Exception ex) 
                    {
                        throw new ArgumentException(ex.Message);
                    }
                }
            }
            if (!m_BrowsableProperties.IsNullOrEmpty())
            {
                // Clear properties to filter the list from scratch BY IAP
                m_PropertyDescriptors.Clear();
                foreach(string propertyname in m_BrowsableProperties) 
                {
                    try 
                    {
                        ShowProperty(allproperties[propertyname]);
                    } 
                    catch (Exception)
                    {
                        throw new ArgumentException("Property not found.", propertyname);
                    }
                }
            }
            m_PropertyDescriptors.Sort(CompareDescriptors);
        }

        /// <summary>
        /// Compare two property descriptors.
        /// </summary>
        static int CompareDescriptors(PropertyDescriptor a, PropertyDescriptor b)
        {
            if (a is null) return b is null ? 0 : -1;
            if (b is null) return 1;
            int value = string.Compare(a.Category, b.Category);
            return value == 0 ? string.Compare(a.DisplayName, b.DisplayName) : value;
        }

        /// <summary>
        /// Allows to hide a set of properties to the parent PropertyGrid.
        /// </summary>
        void HideAttribute(Attribute attribute) 
        {
            var collection = TypeDescriptor.GetProperties(m_Wrapper.SelectedObject, new[] { attribute });
            if(collection.IsNullOrEmpty()) throw new ArgumentException("Attribute not found", attribute.ToString());
            foreach(PropertyDescriptor propertydescriptor in collection) HideProperty(propertydescriptor);
        }

        /// <summary>
        /// Add all the properties that match an attribute to the list of properties to be displayed in the PropertyGrid.
        /// </summary>
        void ShowAttribute(Attribute attribute) 
        {
            var collection = TypeDescriptor.GetProperties(m_Wrapper.SelectedObject,new[] { attribute });
            if (collection.IsNullOrEmpty()) throw new ArgumentException("Attribute not found", attribute.ToString());
            foreach(PropertyDescriptor propertydescriptor in collection) ShowProperty(propertydescriptor);
        }

        /// <summary>
        /// Add a property to the list of properties to be displayed in the PropertyGrid.
        /// </summary>
        void ShowProperty(PropertyDescriptor property) 
        {
            if (!m_PropertyDescriptors.Contains(property)) m_PropertyDescriptors.Add(property);
        }

        /// <summary>
        /// Allows to hide a property to the parent PropertyGrid.
        /// </summary>
        void HideProperty(PropertyDescriptor property) 
        {
            if (m_PropertyDescriptors.Contains(property)) m_PropertyDescriptors.Remove(property);
        }

    }

    #region Internal Classes

    /// <summary>
    /// This class is a wrapper. It contains the object the propertyGrid has to display.
    /// </summary>
    internal class ObjectWrapper : ICustomTypeDescriptor
    {
        /// <summary>
        /// Simple constructor.
        /// </summary>
        /// <param name="obj">A reference to the selected object that will linked to the parent PropertyGrid.</param>
        internal ObjectWrapper(object obj) => SelectedObject = obj;

        /// <summary>
        /// Get or set a reference to the selected object that will linked to the parent PropertyGrid.
        /// </summary>
        public object SelectedObject { get; set; }

        /// <summary>
        /// Get or set a reference to the collection of properties to show in the parent PropertyGrid.
        /// </summary>
        public List<PropertyDescriptor> PropertyDescriptors { get; set; } = new List<PropertyDescriptor>();

        #region ICustomTypeDescriptor Members

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes) => GetProperties();

        public PropertyDescriptorCollection GetProperties() => new PropertyDescriptorCollection(PropertyDescriptors.ToArray(), true);

        public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(SelectedObject, true);

        public string GetClassName() => TypeDescriptor.GetClassName(SelectedObject, true);

        public string GetComponentName() => TypeDescriptor.GetComponentName(SelectedObject, true);

        public TypeConverter GetConverter() => TypeDescriptor.GetConverter(SelectedObject, true);

        public EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(SelectedObject, true);

        public PropertyDescriptor GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(SelectedObject, true);

        public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);

        public EventDescriptorCollection GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(SelectedObject, attributes, true);

        public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(SelectedObject, true);

        public object GetPropertyOwner(PropertyDescriptor pd) => SelectedObject;

        #endregion
    }
    #endregion
}