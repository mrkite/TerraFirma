/** @copyright 2025 Sean Kasun */

#pragma once

const int MinVersion = 88;
const int MaxVersion = 319;

#include "handle.h"
#include "json.h"
#include <vector>
#include <unordered_map>
#include <memory>

class WorldHeader {
  public:
    class Header {
      public:
        Header();
        virtual ~Header();
        int toInt() const;
        double toDouble() const;
        int length() const;
        std::shared_ptr<Header> at(int i) const;
        void setData(uint64_t v);
        void setData(std::string s);
        void append(uint64_t v);
        void append(std::string s);
      private:
        uint64_t dint;
        double ddbl;
        std::string dstr;
        std::vector<std::shared_ptr<Header>> darr;
    };

    WorldHeader();
    virtual ~WorldHeader();
    void load(std::shared_ptr<Handle> handle, int version);
    bool has(const std::string &key) const;
    std::shared_ptr<Header> operator[](const std::string &key) const;
    bool is(const std::string &key) const;
    int treeStyle(int x) const;

  private:
    std::unordered_map<std::string, std::shared_ptr<Header>> data;

    struct Field {
      enum Type {
        BOOLEAN,
        BYTE,
        INT16,
        INT32,
        INT64,
        FLOAT32,
        FLOAT64,
        STRING,
        ARRAY_BYTE,
        ARRAY_INT16,
        ARRAY_INT32,
        ARRAY_STRING,
      };

      explicit Field(std::shared_ptr<JSONData> data);

      std::string name;
      Type type;
      int length, minVersion, maxVersion;
      std::string dynamicLength;
    };

    int getFieldLength(const Field &f);

    std::vector<Field> fields;
};
