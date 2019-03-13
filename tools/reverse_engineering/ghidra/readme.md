Copy NXP_MK22.pspec into /Ghidra/Processors/ARM/data/languages

Merge ARM.ldefs into the existing file, to add this block of XML

<language processor="ARM"
		endian="little"
		size="32"
		variant="NPX MK22"
		version="1.101"
		slafile="ARM7_le.sla"
		processorspec="NXP_MK22.pspec"
		manualindexfile="../manuals/ARM.idx"
		id="ARM:LE:32:NXP_MK22">
	<description>NXP MK22</description>
	<compiler name="default" spec="ARM.cspec" id="default"/>
	<external_name tool="IDA-PRO" name="arm"/>
	<external_name tool="DWARF.register.mapping.file" name="ARMneon.dwarf"/>
</language> 

(Or just overwrite the existing ARM.ldefs with this version)