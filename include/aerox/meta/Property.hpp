// META_IGNORE
#pragma once
#include <functional>
#include "Field.hpp"
#include "Reference.hpp"
#include "Type.hpp"
#include "Value.hpp"
#include "aerox/typedefs.hpp"

namespace aerox::meta
{
    typedef std::function<Value(const Value&)> getterFn;
    typedef std::function<void(const Value&, const Value&)> setterFn;

    template<typename TClass,typename  TPropertyType>
    using TProperty = TPropertyType TClass::*;
    
    struct PropertyAccessorBase
    {
        virtual ~PropertyAccessorBase() = default;
        virtual Reference Get(const Reference& instance) = 0;
        // virtual void Set(const Value& instance,const Value& data) = 0;
    };

    template<typename TClass,typename  TPropertyType>
    struct PropertyAccessor : PropertyAccessorBase
    {
        
        PropertyAccessor(TProperty<TClass,TPropertyType> prop)
        {
            _ptr = prop;
        }

        Reference Get(const Reference& instance) override
        {
            TClass * cls = instance;
            return Reference(cls->*_ptr);
        }
        //
        // void Set(const Value& instance,const Value& data) override
        // {
        //     auto d = *data.GetPtr<TPropertyType>();
        //     instance.GetPtr<TClass>()->*_ptr = d;
        // }
    private:
         TProperty<TClass,TPropertyType> _ptr = nullptr;
    };

    
    
    struct Property : Field
    {
    private:
        Type _type;
        std::shared_ptr<PropertyAccessorBase> _accessor;
    public:
        
        Property(const Type& type, const std::string& name, const std::shared_ptr<PropertyAccessorBase>& accessor);

        template<typename T>
        Reference Get(T * instance);
        //
        // // template<typename T,typename V>
        // // void Set(T * instance, V&& data);
        //
        // template<typename T>
        // Reference Get(T &instance);
        //
        // template<typename T,typename V>
        // void Set(T &instance, V&& data);

        Type GetPropertyType() const;
    };

    template <typename T>
    Reference Property::Get(T* instance)
    {
        return _accessor->Get(Reference(instance));
    }

    // template <typename T, typename V>
    // void Property::Set(T* instance, V&& data)
    // {
    //     auto d = data;
    //     _accessor->Set(instance,&data);
    // }

    // template <typename T>
    // Reference Property::Get(T& instance)
    // {
    //     return Get(&instance);
    // }
    //
    // template <typename T, typename V>
    // void Property::Set(T& instance, V&& data)
    // {
    //     Set(&instance,data);
    // }

    template<typename TClass,typename  TPropertyType>
    inline std::shared_ptr<PropertyAccessorBase> makeAccessor(TProperty<TClass,TPropertyType> prop)
    {
        return std::make_shared<PropertyAccessor<TClass,TPropertyType>>(prop);
    }
}
