FROM debian:jessie

MAINTAINER Mikey <quezlatch@gmail.com>

# based on official mono dockerfile

# add the bits
RUN apt-get update \
	&& apt-get install -y curl build-essential autogen automake libtool \
	&& rm -rf /var/lib/apt/lists/*

#build libuv from source
ADD http://dist.libuv.org/dist/v1.9.0/libuv-v1.9.0.tar.gz /tmp
WORKDIR /tmp
RUN tar -zxf libuv-v1.9.0.tar.gz
WORKDIR libuv-v1.9.0
RUN sh autogen.sh
RUN ./configure
RUN make
RUN make install
RUN apt-get remove -y build-essential autogen automake libtool && apt-get autoremove -y
WORKDIR /tmp
RUN rm -fr libuv*

#install fsharp (from latest mono)
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN echo "deb http://download.mono-project.com/repo/debian wheezy main" | tee /etc/apt/sources.list.d/mono-xamarin.list \
  && echo "deb http://download.mono-project.com/repo/debian wheezy-libjpeg62-compat main" | tee -a /etc/apt/sources.list.d/mono-xamarin.list \
	&& apt-get update \
	&& apt-get install -y mono-devel ca-certificates-mono fsharp nuget \
	&& rm -rf /var/lib/apt/lists/*

#install paket
ADD https://github.com/fsprojects/Paket/releases/download/2.62.3/paket.exe /opt/paket/

#app directory
RUN mkdir -p /opt/app
WORKDIR /opt/app

#paket restore
ONBUILD COPY paket.dependencies paket.lock ./
ONBUILD RUN mono /opt/paket/paket.exe restore

#copy over scripts
ONBUILD COPY *.fsx .

ENTRYPOINT ["fsharpi", "--exec"]
CMD ["app.fsx"]
