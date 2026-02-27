/** @copyright 2025 Sean Kasun */

#pragma once
#include <map>
#include <string>
#include <filesystem>
#include <memory>

class SteamConfig {
  public:
    SteamConfig();
    std::filesystem::path getBase() const;
    std::filesystem::path getTerraria() const;
    std::filesystem::path expand(const char *path) const;

  private:
    struct Tokenizer {
      std::string data;
      size_t pos;
      char next();
      std::string key();
    };
    
    struct Element {
      Element();
      explicit Element(Tokenizer *t);
      std::string find(const std::string &path) const;
    
      std::map<std::string, struct Element> children;
      std::string name, value;
    };

    std::unique_ptr<Element> parsevdf(const std::filesystem::path &base);

    std::filesystem::path steamBase;
    std::filesystem::path terrariaBase;
};
