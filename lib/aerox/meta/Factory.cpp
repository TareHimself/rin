#include "aerox/meta/Factory.hpp"

#include "aerox/json.hpp"
#include "aerox/typedefs.hpp"

namespace aerox::meta {
std::shared_ptr<_INTERNAL_TYPE_FACTORY> _internalGetFactory() {
  static std::shared_ptr<_INTERNAL_TYPE_FACTORY> factory = std::make_shared<
    _INTERNAL_TYPE_FACTORY>();
  return factory;
}

std::vector<std::string> names() {
  const auto factory = _internalGetFactory();
  std::vector<std::string> keys;
  for (const auto &fst : factory->aliases | std::views::keys) {
    keys.push_back(fst);
  }

  return keys;
}

std::vector<Type> types() {
  const auto factory = _internalGetFactory();
  std::vector<Type> types;
  for (const auto &snd : factory->aliases | std::views::values) {
    types.push_back(snd);
  }

  return types;
}

std::vector<std::shared_ptr<Metadata>> values() {
  const auto factory = _internalGetFactory();
  std::vector<std::shared_ptr<Metadata>> values;
  for (const auto &snd : factory->classes | std::views::values) {
    values.push_back(snd);
  }

  return values;
}

std::shared_ptr<Metadata> find(const Type &type) {
  const auto factory = _internalGetFactory();

  if (const auto result = factory->classes.find(type);
    result != factory->classes.end()) {
    return result->second;
  }

  return {};
}

std::shared_ptr<Metadata> find(const std::string &name) {
  const auto factory = _internalGetFactory();
  if (const auto result = factory->aliases.find(name);
    result != factory->aliases.end()) {
    return find(result->second);
  }

  return {};
}

TypeBuilder &TypeBuilder::AddField(
    const std::shared_ptr<Field> &field, const std::set<std::string> &tags) {
  field->FillTagsFrom(tags);
  fields.push_back(field);
  return *this;
}

TypeBuilder &TypeBuilder::AddFunction(const std::string &name,
                                      const functionTypedef &caller,
                                      bool isStatic,
                                      const std::set<std::string> &tags) {
  return AddField(std::make_shared<Function>(name, caller, isStatic), tags);
}

META_DECLARE() {
  using int64 = int64_t;
  using string = std::string;

  META_PRIMITIVE(float)
  META_PRIMITIVE(int)
  META_PRIMITIVE(int64)
  META_PRIMITIVE(bool)
  META_PRIMITIVE(double)
  META_PRIMITIVE(string)
}
}
