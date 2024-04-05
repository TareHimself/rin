// META_IGNORE
#pragma once
#include <ostream>
#include <string>

#include "Macro.hpp"
#include "Utils.hpp"
#include "aerox/containers/TFlags.hpp"

#include <typeindex>

namespace aerox::meta
{
enum ETypeFlags {
  eNone = flagField(0),
  eClass = flagField(1),
  ePrimitive = flagField(2),
  ePointer = flagField(3)
};
    struct Type
    {
    private:
        TFlags<ETypeFlags> _flags;
        std::type_index _typeIndex = typeid(nullptr);
        size_t _size = 0;
    public:

        Type();
        template <typename T>
        static Type Infer();

        bool operator==(const Type& other) const;

        TFlags<ETypeFlags> GetFlags() const;
        std::type_index GetTypeIndex() const;
      size_t GetSize() const;

        friend std::ostream& operator<<(std::ostream &stream,const Type& other)
        {
            stream << "Removed";
            
            return stream;
        }

        struct HashFunction
        {
            size_t operator()(const Type& t) const
            {
                return t.GetTypeIndex().hash_code();
            }
        };
    };

    template <typename T>
    Type Type::Infer()
    {
      Type result;
      result._typeIndex = typeid(T);
      result._size = sizeof(T);

      TFlags<ETypeFlags> flags;

      if(std::is_class_v<T>) {
        flags.Add(ETypeFlags::eClass);
      }

      if(std::is_fundamental_v<T>) {
        flags.Add(ePrimitive);
      }

      if(std::is_pointer_v<T>) {
        flags.Add(ePointer);
      }

      result._flags = flags;

      return result;
    }

    

    
}
