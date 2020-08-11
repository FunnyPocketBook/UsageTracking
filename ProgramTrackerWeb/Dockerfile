FROM node:alpine

RUN mkdir -p /usr/src/UsageTracking
WORKDIR /usr/src/UsageTracking/ProgramTrackerWeb

COPY . /usr/src/UsageTracking/ProgramTrackerWeb
RUN npm install
RUN npm run build
EXPOSE 12502
ENV NUXT_HOST=0.0.0.0
ENV NUXT_PORT=12502

CMD [ "npm", "start" ]