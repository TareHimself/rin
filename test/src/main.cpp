//#include "TArray.hpp"


#include "Managed.hpp"

#include <cstdlib>
#include <iostream>
#include <ostream>


class Ret {
  
};

class Foo {
public:
  void Test() {
    std::cout << "This is foo" << std::endl;
  }
  
};


class Zee : public RefThis<Zee> {
public:
  virtual void F() = 0;
  virtual ~Zee() = default;
};

class Bar : public Foo, public Zee {
  void F() override {
    
  }
};

class K : public Bar {
  
};

void test(const Managed<Ret>& t) {
  
}

void test2(const Managed<Foo>& t) {
  
}

int main(int argc, char **argv) {

  try {
    //auto b = manage<Foo>();
    auto a = manage<K>();
    auto d = a->ToRef();
    a.Clear();
    // test2(a);
    //
    // Managed<Foo> conv = a;
    // auto j = a->ToRef().Reserve();
    // Ref<Foo> ref = a.CastStatic<Foo>().ToRef();
    // int f = 20;
    // float i = try_cast<float>(f);
    // auto d = try_cast<Zee *>(a.Get());
    // auto c = a.CastStatic<Foo>();
    // a.Clear();
    // b.Clear();
    // conv->Test();
  } catch (const std::exception &e) {
    std::cerr << e.what() << std::endl;
    return EXIT_FAILURE;
  }

  return EXIT_SUCCESS;
}
// enum ETestEnum {
//     eOne = flagField(0),
//     eTwo = flagField(1),
//     eThree = flagField(2),
//     eFour = flagField(3),
//     eFive = flagField(4),
//     eSix = flagField(5)
// };
//
// int main(int argc, char **argv) {
//
//   try {
//       TFlags<ETestEnum> flags;
//       TArray<ETestEnum> arr;
//
//       flags |= ETestEnum::eOne;
//       flags |= ETestEnum::eFour;
//       arr.AddAll(ETestEnum::eOne,ETestEnum::eTwo,ETestEnum::eThree);
//       auto c = arr.Find([](ETestEnum a){
//          return a == ETestEnum::eTwo;
//       });
//
//       auto d = *c;
//
//       auto r = flags.Has(ETestEnum::eSix);
//
//       auto r2 = flags.Has(ETestEnum::eFour);
//
//       flags &= ETestEnum::eOne;
//
//       auto r3 = flags.Has(ETestEnum::eOne);
//
//       flags |= ETestEnum::eFive;
//
//
//   } catch (const std::exception &e) {
//     std::cerr << e.what() << std::endl;
//     return EXIT_FAILURE;
//   }
//
//   return EXIT_SUCCESS;
// }
//    TArray<int> test;
//    test.Add(1,2,3,4,5,6,7,8,9,10);
//    auto asStr = test.Map<std::string>([](int & a) {
//      return std::to_string(a);
//    });
//
//    for(auto i : test) {
//
//    }