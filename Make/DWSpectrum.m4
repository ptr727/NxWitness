include(`macros.m4')
DONT_CHANGE(__file__)

FROM ubuntu:trusty

ENV DEBIAN_FRONTEND noninteractive

include(`build_tools.m4')
include(`ruby_2_1_2.m4')
include(`docker_latest.m4')