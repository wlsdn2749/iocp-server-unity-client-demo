#pragma once

// -------------------
//		1차 시도
// -------------------

//struct SListEntry
//{
//	SListEntry* next;
//};
//
//
//struct SListHeader
//{
//	SListEntry* next = nullptr;
//};
//
//// [Data+Node][ ][ ][ ]
//// [Header]
//
//void InitializeHead(SListHeader* header);
//
//// [Header]->[...]
//// [Header]->[Entry]->[...]
//void PushEntrySList(SListHeader* header, SListEntry* entry);
//
//SListEntry* PopEntrySList(SListHeader* header);


// -------------------
//		2차 시도 <- ABA Problem 발생
// -------------------

//struct SListEntry
//{
//	SListEntry* next;
//};
//
//
//struct SListHeader
//{
//	SListEntry* next = nullptr;
//};
//
//// [Data+Node][ ][ ][ ]
//// [Header]
//
//void InitializeHead(SListHeader* header);
//
//// [Header]->[...]
//// [Header]->[Entry]->[...]
//void PushEntrySList(SListHeader* header, SListEntry* entry);
//
//SListEntry* PopEntrySList(SListHeader* header);

// -------------------
//		3차 시도
// -------------------

DECLSPEC_ALIGN(16)
struct SListEntry
{
	SListEntry* next;
};

DECLSPEC_ALIGN(16) // 16*8 128byte 정렬
struct SListHeader
{
	SListHeader()
	{
		alignment = 0;
		region = 0;
	}
	union
	{
		struct
		{
			uint64 alignment; // 64 byte
			uint64 region; // 64byte
		} DUMMYSTRUCTNAME;

		struct
		{
			uint64 depth : 16;
			uint64 sequence : 48; // 여기까지 alignment 대신 사용할것인가?
			uint64 reserved : 4;
			uint64 next : 60; // 여기까지 region으로 사용할 것인가?
		} HeaderX64;
	};
};

// [Data+Node][ ][ ][ ]
// [Header]

void InitializeHead(SListHeader* header);

// [Header]->[...]
// [Header]->[Entry]->[...]
void PushEntrySList(SListHeader* header, SListEntry* entry);

SListEntry* PopEntrySList(SListHeader* header);